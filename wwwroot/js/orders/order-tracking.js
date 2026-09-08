$(function () {
    const $panel = $(".tracking-panel");
    if (!$panel.length) return;

    const $status = $("#tracking-status");
    const order = ["Pending", "Preparing", "Ready", "Served"];

    function draw(status) {
        $status.text(status);
        const currentIndex = order.indexOf(status);
        $(".tracking-step").each(function () {
            const $step = $(this);
            const stepStatus = $step.data("step");
            const index = order.indexOf(stepStatus);
            $step.toggleClass("is-complete", currentIndex >= 0 && index <= currentIndex);
            $step.toggleClass("is-current", stepStatus == status);
        });
    }

    function refresh() {
        $.getJSON($panel.data("status-url"))
        .done(function (result) {
            draw(result.status);
        });
    }

    draw($status.text().trim());
    setInterval(refresh, 5000);
});
