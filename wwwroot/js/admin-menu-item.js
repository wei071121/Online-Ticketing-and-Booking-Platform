(() => {
    const list = document.getElementById("addon-list");
    const addButton = document.getElementById("add-addon-btn");
    const template = document.getElementById("addon-template");

    function reindexAddOns() {
        if (!list) return;

        [...list.querySelectorAll(".addon-row")].forEach((row, index) => {
            row.querySelectorAll("[name]").forEach(input => {
                input.name = input.name.replace(/AddOns\[\d+\]/g, `AddOns[${index}]`);
            });

            row.querySelectorAll("[id]").forEach(input => {
                input.id = input.id.replace(/AddOns_\d+__/g, `AddOns_${index}__`);
            });
        });
    }

    if (list && addButton && template) {
        addButton.addEventListener("click", () => {
            const index = list.querySelectorAll(".addon-row").length;
            list.insertAdjacentHTML(
                "beforeend",
                template.innerHTML.replaceAll("__index__", index)
            );
        });

        list.addEventListener("click", event => {
            const button = event.target.closest(".remove-addon");
            if (!button) return;

            button.closest(".addon-row")?.remove();
            reindexAddOns();
        });
    }

    const photoInput = document.getElementById("menu-photo-input");
    const photoPreview = document.getElementById("menu-photo-preview");

    if (photoInput && photoPreview) {
        photoInput.addEventListener("change", () => {
            photoPreview.innerHTML = "";

            [...photoInput.files].slice(0, 6).forEach(file => {
                if (!file.type.startsWith("image/")) return;

                const tile = document.createElement("div");
                tile.className = "menu-upload-preview";

                const image = document.createElement("img");
                image.src = URL.createObjectURL(file);
                image.alt = file.name;
                image.onload = () => URL.revokeObjectURL(image.src);

                const name = document.createElement("small");
                name.textContent = file.name;

                tile.append(image, name);
                photoPreview.append(tile);
            });
        });
    }
})();
