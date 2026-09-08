(() => {
    const form = document.getElementById("menu-filter-form");
    const search = document.getElementById("menu-search");
    const category = document.getElementById("menu-category");
    const clearButton = document.getElementById("menu-clear");
    const results = document.getElementById("menu-results");
    const chips = [...document.querySelectorAll(".menu-chip[data-category-id]")];

    if (!form || !search || !category || !results) return;

    let timer = null;
    let requestController = null;

    function syncChips() {
        chips.forEach(chip => {
            chip.classList.toggle("active", chip.dataset.categoryId === category.value);
        });
    }

    async function reloadMenu() {
        const params = new URLSearchParams();

        if (search.value.trim())
            params.set("search", search.value.trim());

        if (category.value)
            params.set("categoryId", category.value);

        requestController?.abort();
        requestController = new AbortController();
        results.classList.add("menu-loading");
        syncChips();

        try {
            const suffix = params.toString() ? `?${params}` : "";
            const response = await fetch(`${form.action}${suffix}`, {
                headers: { "X-Requested-With": "XMLHttpRequest" },
                signal: requestController.signal
            });

            if (!response.ok)
                throw new Error("Unable to load menu.");

            results.innerHTML = await response.text();
            history.replaceState(null, "", `${location.pathname}${suffix}`);
        } catch (error) {
            if (error.name !== "AbortError") {
                results.innerHTML =
                    '<div class="alert alert-danger">Unable to update the menu. Please try again.</div>';
            }
        } finally {
            results.classList.remove("menu-loading");
        }
    }

    search.addEventListener("input", () => {
        clearTimeout(timer);
        timer = setTimeout(reloadMenu, 280);
    });

    category.addEventListener("change", reloadMenu);

    chips.forEach(chip => {
        chip.addEventListener("click", () => {
            category.value = chip.dataset.categoryId || "";
            reloadMenu();
        });
    });

    clearButton?.addEventListener("click", () => {
        search.value = "";
        category.value = "";
        reloadMenu();
        search.focus();
    });

    form.addEventListener("submit", event => {
        event.preventDefault();
        reloadMenu();
    });

    syncChips();
})();
