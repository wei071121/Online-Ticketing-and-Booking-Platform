// Customer reservations use market-style time slots while staff walk-in
// seating continues to expose exact operational table numbers.
$(function () {
    const $panel = $("#availability-panel");
    if (!$panel.length) return;

    const url = $panel.data("url");
    const mode = $panel.data("mode");
    const $hiddenTableId = $("#DiningTableId");

    function tableCard(table) {
        const $label = $("<label>").addClass("form-check available-table-option");
        const $input = $("<input>", {
            class: "form-check-input",
            type: "radio",
            name: "table-pick",
            value: table.diningTableId,
            "aria-label": `Select table ${table.tableNumber}`
        });

        $label.append($input);
        $("<span>").addClass("table-card-number").text(table.tableNumber).appendTo($label);
        $("<span>").addClass("table-card-meta").text(`Seats up to ${table.capacity} guest${table.capacity === 1 ? "" : "s"}`).appendTo($label);
        $("<span>").addClass("table-card-status").text("Available").appendTo($label);
        return $label;
    }

    function clearTableSelection() {
        $hiddenTableId.val("");
        $(".available-table-option").removeClass("is-selected");
    }

    if (mode === "reservation") {
        const $form = $("#reservation-wizard");
        const $message = $("#availability-message");
        const $slotList = $("#available-time-slots");
        const $time = $("#ReservationTime");
        const $visitSummary = $("#selected-visit-summary");

        function params() {
            return {
                reservationDate: $("#ReservationDate").val(),
                pax: $("#Pax").val(),
                durationMinutes: 120
            };
        }

        function setFeedback(step, message) {
            $(`[data-step-feedback='${step}']`).text(message || "");
        }

        function showStep(step) {
            $form.addClass("reservation-wizard-active");
            $form.find(".reservation-step").removeClass("is-active");
            $form.find(`.reservation-step[data-step='${step}']`).addClass("is-active");
            $("[data-progress-step]").each(function () {
                const itemStep = Number($(this).data("progress-step"));
                $(this).toggleClass("is-active", itemStep === step);
                $(this).toggleClass("is-complete", itemStep < step);
            });
            $form.find(".wizard-feedback").text("");
            window.scrollTo({ top: Math.max(0, $form.offset().top - 100), behavior: "smooth" });
        }

        function visitIsComplete() {
            const request = params();
            return Boolean(request.reservationDate && request.pax && Number(request.pax) >= 1);
        }

        function clearTimeSelection() {
            $time.val("");
            $slotList.find(".reservation-time-slot").removeClass("is-selected").attr("aria-pressed", "false");
            $visitSummary.empty();
        }

        function renderSlots(response) {
            const slots = response?.slots || [];
            clearTimeSelection();
            $slotList.empty();
            showStep(2);

            if (!slots.length) {
                $message.html('<span class="availability-summary-icon" aria-hidden="true">!</span><span><strong>No times are available on this date.</strong> Please go back and try another day or reduce the party size.</span>');
                return;
            }

            $message.html(`<span class="availability-summary-icon" aria-hidden="true">✓</span><span><strong>${slots.length} available time${slots.length === 1 ? "" : "s"}.</strong> Select the arrival time that suits you.</span>`);
            slots.forEach(function (slot) {
                const label = slot.availableTableCount <= 2 ? "Limited availability" : "Available";
                const $button = $("<button>", {
                    type: "button",
                    class: "reservation-time-slot",
                    "data-time": slot.reservationTime,
                    "data-display-time": slot.displayTime,
                    "aria-pressed": "false"
                });
                $("<strong>").text(slot.displayTime).appendTo($button);
                $("<span>").text(label).appendTo($button);
                $slotList.append($button);
            });
        }

        function loadSlots() {
            if (!visitIsComplete()) {
                showStep(1);
                setFeedback(1, "Please choose a date and valid party size before continuing.");
                return;
            }

            $message.html('<span class="availability-summary-icon" aria-hidden="true">…</span><span>Checking reservation times…</span>');
            $slotList.empty();
            $.get(url, params())
                .done(renderSlots)
                .fail(function () {
                    showStep(2);
                    $message.html('<span class="availability-summary-icon" aria-hidden="true">!</span><span>Unable to check reservation times right now. Please try again.</span>');
                });
        }

        $form.on("click", ".wizard-check-availability", loadSlots);
        $form.on("click", ".wizard-back", function () { showStep(Number($(this).data("previous-step"))); });
        $form.on("click", ".wizard-next", function () {
            if (!$time.val()) {
                setFeedback(2, "Please choose an available arrival time.");
                return;
            }
            const request = params();
            const selectedLabel = $slotList.find(".reservation-time-slot.is-selected").data("display-time");
            $visitSummary.html(
                `<div><span>Date</span><strong>${request.reservationDate}</strong></div>` +
                `<div><span>Arrival</span><strong>${selectedLabel}</strong></div>` +
                `<div><span>Guests</span><strong>${request.pax}</strong></div>` +
                '<div><span>Table</span><strong>Best-fit table assigned by QIGLO</strong></div>');
            showStep(Number($(this).data("next-step")));
        });

        $slotList.on("click", ".reservation-time-slot", function () {
            clearTimeSelection();
            $(this).addClass("is-selected").attr("aria-pressed", "true");
            $time.val($(this).data("time"));
            setFeedback(2, "");
        });

        $("#ReservationDate, #Pax").on("change", function () {
            clearTimeSelection();
            $slotList.empty();
        });

        showStep(1);
    }
    else if (mode === "walkin") {
        const $list = $("#available-tables-list");

        function renderWalkInTables(tables) {
            clearTableSelection();
            $list.empty();
            if (!tables || tables.length === 0) {
                $list.html('<div class="alert alert-warning mb-0">No table is available for this party size right now.</div>');
                return;
            }
            const $container = $("<div>").addClass("availability-list");
            tables.forEach(function (table) { $container.append(tableCard(table)); });
            $list.append($container);
        }

        function refreshWalkInTables() {
            $list.html('<div class="text-secondary small">Checking available tables…</div>');
            $.get(url, { pax: $("#Pax").val() || 1 })
                .done(renderWalkInTables)
                .fail(function () { $list.html('<div class="alert alert-danger mb-0">Unable to check table availability right now.</div>'); });
        }

        let timer = null;
        $("#Pax").on("input change", function () {
            clearTimeout(timer);
            timer = setTimeout(refreshWalkInTables, 350);
        });
        $list.on("change", "input[name='table-pick']", function () {
            clearTableSelection();
            $hiddenTableId.val($(this).val());
            $(this).closest(".available-table-option").addClass("is-selected");
        });
        refreshWalkInTables();
    }
});
