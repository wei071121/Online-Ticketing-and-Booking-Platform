(() => {
    const gallery = document.getElementById("menu-gallery");
    const mainPhoto = document.getElementById("menu-main-photo");

    if (gallery && mainPhoto) {
        gallery.addEventListener("click", event => {
            const button = event.target.closest("[data-photo-src]");
            if (!button) return;

            mainPhoto.src = button.dataset.photoSrc;
            gallery.querySelectorAll(".menu-gallery-button").forEach(item => item.classList.remove("active"));
            button.classList.add("active");
        });
    }

    const form = document.getElementById("menu-customize-form");
    const total = document.getElementById("menu-live-total");
    const quantity = document.getElementById("menu-quantity");

    if (!form || !total || !quantity) return;

    const basePrice = Number.parseFloat(form.dataset.basePrice || "0");

    function updateTotal() {
        const selectedAddOns = [...form.querySelectorAll(".menu-addon-check:checked")]
            .reduce((sum, input) => sum + Number.parseFloat(input.dataset.price || "0"), 0);
        const qty = Math.max(1, Math.min(20, Number.parseInt(quantity.value || "1", 10) || 1));
        total.textContent = `RM ${((basePrice + selectedAddOns) * qty).toFixed(2)}`;
    }

    form.addEventListener("change", updateTotal);
    form.addEventListener("input", event => {
        if (event.target === quantity) updateTotal();
    });

    updateTotal();
})();
