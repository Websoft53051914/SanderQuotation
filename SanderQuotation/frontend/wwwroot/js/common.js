const inputs = document.querySelectorAll('.focusinput');
if (inputs.length > 0) {
    inputs[0].focus();
}

/**
 * Generates a UUID (Universally Unique Identifier).
 * @returns {string} A UUID string.
 */
function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

/**
 * Formats a file size in bytes into a human-readable string.
 * @param {number} bytes - The file size in bytes.
 * @returns {string} The formatted file size.
 */
function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}

/**
 * Formats a UNIX timestamp into a human-readable date string.
 * @param {any} timestamp - The UNIX timestamp to format.
 * @returns {string} The formatted date string.
 */
function formatDate(timestamp) {
    if (!timestamp) return "-";
    const date = new Date(timestamp * 1000);
    return date.toLocaleDateString('zh-TW') + ' ' + date.toLocaleTimeString('zh-TW');
}

function numberOnly(value)
{
    value = value.replace(/[^0-9]/g, '')
    return value
}

//列表資料明細檢視頁
function openDetailView(controllerName, id)
{

    if (!id)
        return

    var url = '/' + controllerName + '/DetailView?id=' + id

    window.open(url, '_blank')
}

function clearForm(formId)
{
    const form = document.getElementById(formId)
    if (!form) return

    // Reset form
    form.reset()   

    form.querySelectorAll("select").forEach(select =>
    {
        if (!select.choices) return

        // 取 reset 後 select 的實際值（通常是第一個 option）
        const v = String(select.value ?? '')

        // 單選：直接設回 reset 後的值，避免 UI 不同步
        if (!select.multiple)
        {
            select.choices.setChoiceByValue(v)
        } else
        {
            // 多選才用 removeActiveItems 比較合理
            select.choices.removeActiveItems()
        }
    })
}

function setChoicesOptions(selectOrSelector, list, { valueKey = 'Value', labelKey = 'Text', replace = true } = {})
{
    const el = (typeof selectOrSelector === 'string')
        ? document.querySelector(selectOrSelector)
        : selectOrSelector

    if (!el) throw new Error(`Select element not found: ${selectOrSelector}`)
    if (!el.choices) throw new Error(`Choices instance not found on element: ${el.id || el.name || el}`)

    const options = (list ?? []).map(item => ({
        value: item[valueKey],
        label: item[labelKey]
    }))

    el.choices.clearChoices()
    el.choices.setChoices(options, 'value', 'label', replace)
}

function getCommonCodeList(url='',codeType='',parentType='',parentCode='') {
    return $.ajax({
        url,
        method: 'POST',
        data: {
            CodeType: codeType,
            ParentType: parentType,
            ParentCode: parentCode
        }
    });
}

//渲染 List JSON資料為 badge 樣式
//支援：JSON Array、JSON Object、純字串（直接渲染為單一 badge）
function renderTableTextBadges(JsonStr)
{

    if (!JsonStr || typeof JsonStr !== "string")
    {
        return ""
    }

    // XSS escape function
    const escapeHtml = (unsafe) =>
    {
        return String(unsafe)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;")
    }

    // 產生單一 badge
    const makeBadge = (text) =>
        `<span class="badge bg-success-subtle text-success text-uppercase me-1" style="font-size:0.75rem;">${escapeHtml(text)}</span>`

    try
    {
        const parsed = JSON.parse(JsonStr)
        let badges = []

        // 如果是 Array
        if (Array.isArray(parsed))
        {
            parsed.forEach(item =>
            {
                badges.push(makeBadge(item))
            })
        }

        // 如果是 Object
        else if (typeof parsed === "object" && parsed !== null)
        {
            Object.entries(parsed).forEach(([key, value]) =>
            {
                badges.push(
                    `<span class="badge bg-success-subtle text-success text-uppercase me-1" style="font-size:0.75rem;">${escapeHtml(key)} : ${escapeHtml(value)}</span>`
                )
            })
        }

        // JSON.parse 成功但結果是純字串（如輸入為 '"active"'）
        else if (typeof parsed === "string")
        {
            badges.push(makeBadge(parsed))
        }

        return badges.join("")

    } catch (e)
    {
        // JSON parse 失敗 → 視為純字串直接渲染為 badge
        return makeBadge(JsonStr)
    }
}


//渲染 List boolean 為 checkbox 樣式
function renderCheckboxWithText(value, options = {})
{

    const {
        trueText = "是",
        //disabled = false
    } = options

    if (value === null || value === undefined)
    {
        return ""
    }

    const isChecked = Boolean(value)

    return `
                       <div class="form-check d-flex align-items-center justify-content-start gap-1">
                    <input
                        class="form-check-input"
                        type="checkbox"
                        ${isChecked ? "checked" : ""}
                        style="pointer-events: none;"
                    >
                    
                </div>
            `
}

function formatString(template, ...values) {
    return template.replace(/{(\d+)}/g, (match, index) => values[index]);
}