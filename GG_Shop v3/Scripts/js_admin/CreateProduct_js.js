// Scripts/js_admin/CreateProduct_js.js
// Vanilla JS for product form: tag inputs (colors/sizes) and cartesian variants generation
(function () {
    // Run after DOM ready
    function ready(fn) {
        if (document.readyState !== 'loading') fn();
        else document.addEventListener('DOMContentLoaded', fn);
    }

    ready(function () {
        // --- config / suggestions lists (you can change defaults) ---
        const colorSuggestions = ["Black", "White", "Red", "Blue", "Green", "Gold", "Silver"];
        const sizeSuggestions = ["S", "M", "L", "XL", "XXL", "64GB", "128GB", "256GB"];

        // elements (must exist in the DOM)
        const colorsInput = document.getElementById('colorsInput');
        const colorsText = document.getElementById('colorsText');
        const colorsSuggestionsBox = document.getElementById('colorsSuggestions');

        const sizesInput = document.getElementById('sizesInput');
        const sizesText = document.getElementById('sizesText');
        const sizesSuggestionsBox = document.getElementById('sizesSuggestions');

        const variantsTableBody = document.querySelector('#variantsTable tbody');
        const noVariantsNote = document.getElementById('noVariantsNote');

        if (!colorsInput || !sizesInput || !variantsTableBody || !noVariantsNote) {
            // required elements missing — nothing to do
            return;
        }

        // internal sets (unique)
        let colors = []; // array of strings
        let sizes = [];  // array of strings

        // helpers
        function normalize(s) { return (s || '').trim(); }
        function addTag(container, list, value, renderFn) {
            value = normalize(value);
            if (!value) return;
            if (list.indexOf(value) !== -1) return; // avoid duplicate
            list.push(value);
            renderTags(container, list, renderFn);
            generateVariants();
        }
        function removeTag(container, list, value, renderFn) {
            const idx = list.indexOf(value);
            if (idx === -1) return;
            list.splice(idx, 1);
            renderTags(container, list, renderFn);
            generateVariants();
        }
        function renderTags(container, list, renderFn) {
            // keep input as last child
            const input = container.querySelector('input');
            container.innerHTML = '';
            list.forEach(v => {
                const tag = document.createElement('span');
                tag.className = 'tag';
                tag.innerHTML = '<span>' + v + '</span><span class="remove" data-value="' + v + '">&times;</span>';
                container.appendChild(tag);
            });
            container.appendChild(input);
            input.value = '';
            input.focus();
        }

        // suggestions rendering
        function showSuggestions(box, items, onSelect) {
            box.innerHTML = '';
            if (!items || items.length === 0) { box.style.display = 'none'; return; }
            items.forEach(it => {
                const div = document.createElement('div');
                div.textContent = it;
                div.addEventListener('click', () => { onSelect(it); box.style.display = 'none'; });
                box.appendChild(div);
            });
            box.style.display = 'block';
        }
        function filterSuggestions(list, q) {
            if (!q) return list.slice(0, 8);
            q = q.toLowerCase();
            return list.filter(x => x.toLowerCase().indexOf(q) !== -1).slice(0, 8);
        }

        // keyboard handling on tag inputs
        colorsText.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                addTag(colorsInput, colors, colorsText.value, renderColors);
            } else if (e.key === 'Backspace' && this.value === '') {
                if (colors.length) removeTag(colorsInput, colors, colors[colors.length - 1], renderColors);
            }
        });
        sizesText.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                addTag(sizesInput, sizes, sizesText.value, renderSizes);
            } else if (e.key === 'Backspace' && this.value === '') {
                if (sizes.length) removeTag(sizesInput, sizes, sizes[sizes.length - 1], renderSizes);
            }
        });

        // suggestions show while typing
        colorsText.addEventListener('input', function () {
            const q = this.value.trim();
            const items = filterSuggestions(colorSuggestions, q).filter(x => colors.indexOf(x) === -1);
            showSuggestions(colorsSuggestionsBox, items, (val) => addTag(colorsInput, colors, val, renderColors));
        });
        sizesText.addEventListener('input', function () {
            const q = this.value.trim();
            const items = filterSuggestions(sizeSuggestions, q).filter(x => sizes.indexOf(x) === -1);
            showSuggestions(sizesSuggestionsBox, items, (val) => addTag(sizesInput, sizes, val, renderSizes));
        });

        // click on remove tag (event delegation)
        colorsInput.addEventListener('click', function (e) {
            if (e.target.classList.contains('remove')) {
                const v = e.target.getAttribute('data-value');
                removeTag(colorsInput, colors, v, renderColors);
            } else {
                colorsText.focus();
            }
        });
        sizesInput.addEventListener('click', function (e) {
            if (e.target.classList.contains('remove')) {
                const v = e.target.getAttribute('data-value');
                removeTag(sizesInput, sizes, v, renderSizes);
            } else {
                sizesText.focus();
            }
        });

        // render functions
        function renderColors() { renderTags(colorsInput, colors, renderColors); colorsSuggestionsBox.style.display = 'none'; }
        function renderSizes() { renderTags(sizesInput, sizes, renderSizes); sizesSuggestionsBox.style.display = 'none'; }

        // clicking outside suggestion boxes closes them
        document.addEventListener('click', function (e) {
            if (!colorsInput.contains(e.target)) colorsSuggestionsBox.style.display = 'none';
            if (!sizesInput.contains(e.target)) sizesSuggestionsBox.style.display = 'none';
        });

        // Generate Cartesian product and render variants table
        function generateVariants() {
            // clear table
            variantsTableBody.innerHTML = '';

            if (colors.length === 0 || sizes.length === 0) {
                noVariantsNote.style.display = 'block';
                return;
            }
            noVariantsNote.style.display = 'none';

            let idx = 0;
            // optional: get Title to build default SKU pattern
            const titleInput = document.querySelector('input[name="Title"]');
            const titleVal = titleInput ? titleInput.value : 'SKU';
            for (let c of colors) {
                for (let s of sizes) {
                    const row = document.createElement('tr');

                    // default sku generation: TITLE-COLOR-SIZE
                    const defaultSku = titleVal + '-' + c.replace(/\s+/g, '').toUpperCase() + '-' + s.replace(/\s+/g, '').toUpperCase();

                    row.innerHTML = ''
                        + '<td>' + escapeHtml(c) + '<input type="hidden" name="skus[' + idx + '].Color" value="' + escapeHtml(c) + '" /></td>'
                        + '<td>' + escapeHtml(s) + '<input type="hidden" name="skus[' + idx + '].Size" value="' + escapeHtml(s) + '" /></td>'
                        + '<td><input class="form-control" name="skus[' + idx + '].Sku" value="' + escapeHtml(defaultSku) + '" /></td>'
                        + '<td><input type="number" class="form-control" name="skus[' + idx + '].Quantity" value="0" min="0" /></td>'
                        + '<td><input type="number" step="0.01" class="form-control" name="skus[' + idx + '].Price" value="0" /></td>'
                        + '<td class="text-center"><button type="button" class="btn btn-sm btn-danger remove-variant" data-idx="' + idx + '">Remove</button></td>';

                    variantsTableBody.appendChild(row);
                    idx++;
                }
            }
        }

        // remove-variant handler
        variantsTableBody.addEventListener('click', function (e) {
            if (e.target.classList.contains('remove-variant')) {
                e.target.closest('tr').remove();
                reindexVariants();
            }
        });

        function reindexVariants() {
            const rows = Array.from(variantsTableBody.querySelectorAll('tr'));
            rows.forEach((r, i) => {
                r.querySelectorAll('input').forEach(inp => {
                    const name = inp.getAttribute('name') || '';
                    const newName = name.replace(/skus\[\d+\]/, 'skus[' + i + ']');
                    inp.setAttribute('name', newName);
                });
            });
            if (rows.length === 0) noVariantsNote.style.display = 'block';
        }

        // helper escape
        function escapeHtml(str) {
            if (!str) return '';
            return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
        }

        // expose for debug if needed
        window._variantHelpers = { addTag: addTag, generateVariants: generateVariants, colors: colors, sizes: sizes };
    }); // ready
})(); // IIFE

