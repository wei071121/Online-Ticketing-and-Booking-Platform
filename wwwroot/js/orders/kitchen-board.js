$(function () {
    const $board = $("#kitchen-board");
    if (!$board.length) return;

    const $message = $("#kitchen-message");
    const token = $("#kitchen-token-form input[name='__RequestVerificationToken']").val();

    function updateStatusSummary() {
        $("[data-kitchen-status-count]").each(function () {
            const $summary = $(this);
            const status = String($summary.data("kitchen-status-count")).toLowerCase();
            $summary.find("strong").text($board.find(`.status-${status}`).length);
        });
    }

    function loadBoard() {
        $board.load($board.data("board-url"), function (_html, status) {
            if (status == "error") {
                $message.html('<div class="alert alert-danger">Unable to load the kitchen board.</div>');
            }
            else {
                $("#kitchen-last-updated").text(`Updated ${new Date().toLocaleTimeString()}`);
                updateStatusSummary();
            }
        });
    }

    $board.on("click", ".kitchen-status-button", function () {
        const $button = $(this).prop("disabled", true);

        $.post($board.data("update-url"), {
            orderId: $button.data("order-id"),
            newStatus: $button.data("new-status"),
            __RequestVerificationToken: token
        })
        .done(function (result) {
            $message.html(`<div class="alert alert-success">${result.message}</div>`);
            loadBoard();
        })
        .fail(function (xhr) {
            const text = xhr.responseJSON?.message ?? "Update failed.";
            $message.html(`<div class="alert alert-danger">${text}</div>`);
            $button.prop("disabled", false);
        });
    });

    loadBoard();
    setInterval(loadBoard, 10000);
});
