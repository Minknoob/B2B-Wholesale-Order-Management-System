(() => {
    "use strict";

    const tableBody = document.getElementById("orderLinesBody");
    const addLineBtn = document.getElementById("btnAddOrderLine");
    const grandTotalEl = document.getElementById("orderGrandTotal");
    const distinctCountEl = document.getElementById("orderDistinctCount");
    const totalUnitsEl = document.getElementById("orderTotalUnits");
    const duplicateNoticeEl = document.getElementById("clientDuplicateWarning");
    const orderForm = document.getElementById("createOrderForm");

    if (!tableBody || !Array.isArray(window.b2bCatalogItems)) {
        return;
    }

    const catalogMap = new Map();
    window.b2bCatalogItems.forEach((item) => {
        catalogMap.set(Number(item.itemId), item);
    });

    const currencyFormatter = new Intl.NumberFormat("vi-VN");

    function formatVnd(amount) {
        if (!Number.isFinite(amount)) {
            return "0 ₫";
        }
        return `${currencyFormatter.format(amount)} ₫`;
    }

    function buildSelectOptionsHtml(selectedItemId) {
        const defaultSelected = !selectedItemId || Number(selectedItemId) <= 0 ? " selected" : "";
        let html = `<option value="0"${defaultSelected}>-- Chọn mặt hàng --</option>`;

        window.b2bCatalogItems.forEach((item) => {
            const id = Number(item.itemId);
            const isSelected = id === Number(selectedItemId) ? " selected" : "";
            const isOut = Number(item.stockQuantity) <= 0;
            const disabledAttr = isOut && !isSelected ? " disabled" : "";
            const stockSuffix = isOut
                ? " [Hết hàng]"
                : ` (Tồn: ${item.stockQuantity})`;

            const escapedName = escapeHtml(`${item.itemName} — ${item.size}${stockSuffix}`);
            html += `<option value="${id}"${isSelected}${disabledAttr}>${escapedName}</option>`;
        });

        return html;
    }

    function escapeHtml(text) {
        const div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }

    function createRowElement(defaultItemId, defaultQuantity) {
        const tr = document.createElement("tr");
        tr.className = "order-line-row";
        tr.innerHTML = `
            <td class="text-secondary fw-semibold tabular-nums line-index">1</td>
            <td>
                <select class="form-select item-select" required>
                    ${buildSelectOptionsHtml(defaultItemId)}
                </select>
            </td>
            <td class="text-secondary line-size">—</td>
            <td class="text-end tabular-nums line-stock">—</td>
            <td class="text-end tabular-nums line-unit-price">0 ₫</td>
            <td style="width: 140px;">
                <input type="number" class="form-control text-end tabular-nums qty-input" min="1" step="1" value="${Number(defaultQuantity) || 1}" required />
            </td>
            <td class="text-end fw-semibold tabular-nums line-subtotal">0 ₫</td>
            <td class="text-end">
                <button type="button" class="btn btn-outline-danger btn-sm btn-remove-line" title="Xóa dòng này" aria-label="Xóa dòng sản phẩm">
                    Xóa
                </button>
            </td>
        `;
        return tr;
    }

    function reindexRows() {
        const rows = Array.from(tableBody.querySelectorAll("tr.order-line-row"));
        rows.forEach((row, index) => {
            const indexCell = row.querySelector(".line-index");
            if (indexCell) {
                indexCell.textContent = String(index + 1);
            }

            const select = row.querySelector("select.item-select");
            if (select) {
                select.name = `Lines[${index}].ItemID`;
                select.id = `Lines_${index}__ItemID`;
            }

            const qtyInput = row.querySelector("input.qty-input");
            if (qtyInput) {
                qtyInput.name = `Lines[${index}].Quantity`;
                qtyInput.id = `Lines_${index}__Quantity`;
            }

            const removeBtn = row.querySelector(".btn-remove-line");
            if (removeBtn) {
                removeBtn.disabled = rows.length <= 1;
            }
        });
    }

    function recalculateAll() {
        const rows = Array.from(tableBody.querySelectorAll("tr.order-line-row"));
        let grandTotal = 0;
        let totalUnits = 0;
        const aggregatedByItem = new Map();

        rows.forEach((row) => {
            const select = row.querySelector("select.item-select");
            const qtyInput = row.querySelector("input.qty-input");
            const sizeCell = row.querySelector(".line-size");
            const stockCell = row.querySelector(".line-stock");
            const unitPriceCell = row.querySelector(".line-unit-price");
            const subtotalCell = row.querySelector(".line-subtotal");

            const itemId = Number(select ? select.value : 0);
            const rawQty = Number(qtyInput ? qtyInput.value : 0);
            const validQty = Number.isInteger(rawQty) && rawQty > 0 ? rawQty : 0;
            const item = catalogMap.get(itemId);

            if (item) {
                sizeCell.textContent = item.size;
                stockCell.textContent = currencyFormatter.format(item.stockQuantity);
                unitPriceCell.textContent = formatVnd(item.price);

                const lineTotal = validQty * Number(item.price);
                subtotalCell.textContent = formatVnd(lineTotal);

                grandTotal += lineTotal;
                totalUnits += validQty;

                const prevQty = aggregatedByItem.get(itemId) || 0;
                aggregatedByItem.set(itemId, prevQty + validQty);
            } else {
                sizeCell.textContent = "—";
                stockCell.textContent = "—";
                unitPriceCell.textContent = "0 ₫";
                subtotalCell.textContent = "0 ₫";
            }
        });

        if (grandTotalEl) {
            grandTotalEl.textContent = formatVnd(grandTotal);
        }
        if (distinctCountEl) {
            distinctCountEl.textContent = String(aggregatedByItem.size);
        }
        if (totalUnitsEl) {
            totalUnitsEl.textContent = currencyFormatter.format(totalUnits);
        }

        if (duplicateNoticeEl) {
            const warnings = [];
            aggregatedByItem.forEach((combinedQty, itemId) => {
                const item = catalogMap.get(itemId);
                if (item && combinedQty > Number(item.stockQuantity)) {
                    warnings.push(
                        `Mặt hàng "${item.itemName}" đang có tổng số lượng đặt (${combinedQty}) vượt quá tồn kho hiện tại (${item.stockQuantity}).`
                    );
                }
            });

            if (warnings.length > 0) {
                duplicateNoticeEl.textContent = warnings.join(" ");
                duplicateNoticeEl.classList.remove("d-none");
            } else {
                duplicateNoticeEl.textContent = "";
                duplicateNoticeEl.classList.add("d-none");
            }
        }
    }

    function pickNextSuggestedItemId() {
        const usedIds = new Set(
            Array.from(tableBody.querySelectorAll("select.item-select"))
                .map((s) => Number(s.value))
                .filter((id) => id > 0)
        );

        const unusedAvailable = window.b2bCatalogItems.find(
            (item) => Number(item.stockQuantity) > 0 && !usedIds.has(Number(item.itemId))
        );
        if (unusedAvailable) {
            return Number(unusedAvailable.itemId);
        }

        const firstInStock = window.b2bCatalogItems.find((item) => Number(item.stockQuantity) > 0);
        return firstInStock ? Number(firstInStock.itemId) : 0;
    }

    if (addLineBtn) {
        addLineBtn.addEventListener("click", () => {
            const nextItemId = pickNextSuggestedItemId();
            const newRow = createRowElement(nextItemId, 1);
            tableBody.appendChild(newRow);
            reindexRows();
            recalculateAll();

            const select = newRow.querySelector("select.item-select");
            if (select) {
                select.focus();
            }
        });
    }

    tableBody.addEventListener("change", (event) => {
        if (event.target.matches("select.item-select") || event.target.matches("input.qty-input")) {
            recalculateAll();
        }
    });

    tableBody.addEventListener("input", (event) => {
        if (event.target.matches("input.qty-input")) {
            recalculateAll();
        }
    });

    tableBody.addEventListener("click", (event) => {
        const removeBtn = event.target.closest(".btn-remove-line");
        if (!removeBtn) {
            return;
        }

        const rows = tableBody.querySelectorAll("tr.order-line-row");
        if (rows.length <= 1) {
            return;
        }

        const row = removeBtn.closest("tr.order-line-row");
        if (row) {
            row.remove();
            reindexRows();
            recalculateAll();
        }
    });

    if (orderForm) {
        orderForm.addEventListener("submit", () => {
            reindexRows();
        });
    }

    reindexRows();
    recalculateAll();
})();
