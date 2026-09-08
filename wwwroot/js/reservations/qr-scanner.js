$(function () {
    const input = document.getElementById("BookingCode");
    document.querySelectorAll(".use-code-button").forEach(function (button) {
        button.addEventListener("click", function () {
            input.value = button.dataset.code || "";
            input.focus();
        });
    });

    const startButton = document.getElementById("start-qr-scan");
    const stopButton = document.getElementById("stop-qr-scan");
    const panel = document.getElementById("qr-scanner-panel");
    const video = document.getElementById("qr-video");
    const status = document.getElementById("qr-scan-status");
    let stream = null;
    let scanning = false;
    let detector = null;

    function stopCamera(message) {
        scanning = false;
        if (stream) {
            stream.getTracks().forEach(track => track.stop());
            stream = null;
        }
        video.srcObject = null;
        stopButton.classList.add("d-none");
        startButton.classList.remove("d-none");
        if (message) status.textContent = message;
    }

    async function scanFrame() {
        if (!scanning || !detector) return;
        try {
            if (video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
                const codes = await detector.detect(video);
                const value = codes[0]?.rawValue?.trim().toUpperCase();
                if (value && /^QIGLO-[0-9]{6,12}$/.test(value)) {
                    input.value = value;
                    input.focus();
                    stopCamera(`Booking code ${value} scanned successfully.`);
                    panel.classList.remove("d-none");
                    return;
                }
            }
        } catch {
            status.textContent = "The camera image could not be read. You can still enter the booking code manually.";
        }
        requestAnimationFrame(scanFrame);
    }

    startButton.addEventListener("click", async function () {
        if (!("BarcodeDetector" in window) || !navigator.mediaDevices?.getUserMedia) {
            panel.classList.remove("d-none");
            status.textContent = "QR camera scanning is not supported by this browser. Use current Edge/Chrome or enter the code manually.";
            return;
        }

        try {
            detector = new BarcodeDetector({ formats: ["qr_code"] });
            stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: { ideal: "environment" } },
                audio: false
            });
            video.srcObject = stream;
            await video.play();
            scanning = true;
            panel.classList.remove("d-none");
            startButton.classList.add("d-none");
            stopButton.classList.remove("d-none");
            status.textContent = "Point the camera at a QIGLO QR code.";
            requestAnimationFrame(scanFrame);
        } catch {
            panel.classList.remove("d-none");
            status.textContent = "Camera access was unavailable. Allow camera permission or enter the booking code manually.";
            stopCamera();
        }
    });

    stopButton.addEventListener("click", () => stopCamera("Camera stopped."));
    window.addEventListener("pagehide", () => stopCamera());
});
