(() => {
    const data = window.qigloDashboard;
    if (!data) return;

    function drawBars(canvas, points, color, currency) {
        if (!canvas) return;
        const ratio = window.devicePixelRatio || 1;
        const width = Math.max(320, canvas.clientWidth);
        const height = Number(canvas.getAttribute("height")) || 230;
        canvas.width = width * ratio;
        canvas.height = height * ratio;
        const ctx = canvas.getContext("2d");
        ctx.scale(ratio, ratio);
        ctx.clearRect(0, 0, width, height);

        if (!points.length) {
            ctx.fillStyle = "#68737d";
            ctx.font = "14px Segoe UI";
            ctx.fillText("No data available yet.", 16, 34);
            return;
        }

        const max = Math.max(1, ...points.map(point => Number(point.value)));
        const left = 45, bottom = 36, top = 15, gap = 12;
        const chartHeight = height - top - bottom;
        const barWidth = Math.max(18, (width - left - gap * (points.length + 1)) / points.length);
        ctx.font = "12px Segoe UI";
        ctx.textAlign = "center";
        points.forEach((point, index) => {
            const value = Number(point.value);
            const barHeight = chartHeight * value / max;
            const x = left + gap + index * (barWidth + gap);
            const y = top + chartHeight - barHeight;
            ctx.fillStyle = color;
            ctx.fillRect(x, y, barWidth, barHeight);
            ctx.fillStyle = "#68737d";
            ctx.fillText(point.label.length > 12 ? point.label.slice(0, 11) + "…" : point.label, x + barWidth / 2, height - 14);
            ctx.fillStyle = "#1f2933";
            ctx.fillText(currency ? "RM " + value.toFixed(0) : value.toString(), x + barWidth / 2, Math.max(12, y - 5));
        });
    }

    const render = () => {
        drawBars(document.getElementById("sales-chart"), data.sales || [], "#2f6f5e", true);
        drawBars(document.getElementById("popular-chart"), data.popular || [], "#d59b3d", false);
    };
    render();
    window.addEventListener("resize", render);
})();
