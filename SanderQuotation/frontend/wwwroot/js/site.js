// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function getProjectHealthStatusColor(healthStatus) {
    let healthStatusColor = 'green';
    switch (healthStatus) {
        case 'G':
            healthStatusColor = 'green';
            break;
        case 'Y':
            healthStatusColor = 'yellow';
            break;
        case 'R':
            healthStatusColor = 'red';
            break;
        default:
            healthStatusColor = 'green';
            break;
    }
    return healthStatusColor;
}

function handleAvatarError(element) {
    const $this = $(element);
    $this.hide(); // 隱藏載入失敗的圖片

    const name = $this.data('name') || "User"; // 取得 data-name，若無則預設 User

    const $div = $this.closest('.member-avatar');

    // 檢查是否已經添加過文字頭像，避免重複添加
    if ($div.find('.avatar-title').length === 0) {
        $div.append(`
                               <div class="avatar-title rounded-circle bg-secondary">
                                                    ${name.at(0)}
                                                </div>
                    `);
    }
}

function initDynamicDropdowns(tableId = 'data-list-data') {
    // 只初始化表格列內的 dropdown，避免覆蓋 topbar 等全域 dropdown 的 placement
    var dropdownElements = document.querySelectorAll(`#${tableId} [data-bs-toggle="dropdown"]`)

    dropdownElements.forEach(function (dropdownToggleEl) {
        // 為了避免重複初始化，可以檢查是否已經有實例
        var instance = bootstrap.Dropdown.getInstance(dropdownToggleEl)
        if (instance) instance.dispose()
        if (!instance) {
            // 2. 重新初始化
            new bootstrap.Dropdown(dropdownToggleEl, {
                popperConfig: {
                    placement: 'bottom-start',
                    strategy: 'fixed', // 脫離父層 table 的 overflow 限制
                    modifiers: [
                        {
                            name: 'flip',
                            options: {
                                fallbackPlacements: ['top-start'] // 優先嘗試往上彈
                            }
                        },
                        {
                            name: 'preventOverflow',
                            options: {
                                boundary: 'viewport' // 以視窗為界
                            }
                        }
                    ]
                }
            })
        }
    })
}

/**
 * 產生選單 HTML
 * @param {Array} menuData - JSON 中的 Data 陣列
 * @param {String} menuType - 選單類別
 * @returns {string} 產生的 HTML 字串
 */
function generateMenuHtml(menuData,menuType) {
    let html = '';

    menuData.forEach(item => {
        if (!item.IsVisible) {
            return;
        }
        if (item.MenuLevel === 1) {
//            // 第一層：Dropdown Header
//            html += `<h6 class="dropdown-header">
//    ${item.MenuName}
//</h6>`;
        } else if (item.MenuLevel === 2) {
            // 第二層：Dropdown Item
            let url = 'javascript:void(0);';
            const menuCode = item.MenuCode || '';
            if (item.Url) {
                // 建立一個 URL 物件 (第二參數 window.location.origin 是為了處理相對路徑)
                const fullUrl = new URL(item.Url, window.location.origin);

                // 使用 searchParams.set 會自動處理拼接邏輯
                fullUrl.searchParams.set("systemCode", item.SystemCode);
                fullUrl.searchParams.set("moduleCode", item.ModuleCode);
                fullUrl.searchParams.set("breadcrumb", item.Breadcrumb);
                fullUrl.searchParams.set("menuCode", item.MenuCode);

                // 如果你只需要路徑+參數 (例如 /Project?a=1&b=2)，用 pathname + search
                url = fullUrl.pathname + fullUrl.search;
            }
            
            if (menuType == 'MyNotice') {
                html += `<a href="${url}" class="dropdown-item notify-item language">
    <span class="align-middle">${item.MenuName}</span>
    <span class="align-middle notifications_Count" style="color:red;"></span>
</a>`;
            }
            else {
                if (menuCode == 'accLogout') {
                    html += `<a id="btnLogout" href="${url}" class="dropdown-item notify-item language">
    <span class="align-middle">
        ${item.MenuName}
    </span>
</a>`;
                }
                else {
                    html += `<a href="${url}" class="dropdown-item notify-item language">
    <span class="align-middle">
        ${item.MenuName}
    </span>
</a>`;
                }
                
            }
            
        }

        // 遞迴處理子選單 (SubMenu)
        if (item.SubMenu && item.SubMenu.length > 0) {
            html += generateMenuHtml(item.SubMenu);
        }
    });

    return html;
}
/**
 * 取得儲存或刪除 API 回應的 Swal Alert 資訊
 * @param {any} response - API 回應物件
 * @param {any} defaultSuccessfulMsg - 預設成功訊息，當 API 回應中沒有提供 ReturnCode 時使用
 * @param {any} defaultFailMsg - 預設失敗訊息，當 API 回應中沒有提供 ReturnCode 時使用
 * @returns
 */
function getSaveAPIResponseSwalAlertInfo(response, defaultSuccessfulMsg = '', defaultFailMsg = '') {
    // 1. 判斷是否成功 (保持原有的 Data.IsSuccess 邏輯)
    const isSuccess = response?.Success === true &&
        (response.Data === '' || (response?.Data?.IsSuccess || '').toLowerCase() === 'y');

    // 2. 決定 Icon
    let alertIcon = 'error'; // 預設為 error
    if (isSuccess) {
        alertIcon = 'success';
    } else if (response?.Message && response?.Success === false) {
        // ⭐ 新增需求：有 Message 且 Success 為 false 時，顯示 warning
        alertIcon = 'warning';
    } else {
        // 否則根據 Errors 裡的 AlertLevel，若無則維持 error
        alertIcon = (response?.Errors?.AlertLevel || 'error').toLowerCase();
    }

    // 3. 決定 Title
    let title = '';
    if (isSuccess) {
        title = response?.Data?.ReturnCode || defaultSuccessfulMsg;
    } else {
        // 業務錯誤（JsonValidFail 夾帶 DispatcherReturnMsg）：優先顯示 ReturnMsg
        title = response?.Errors?.ReturnMsg || response?.Message || response?.Errors?.ReturnCode || defaultFailMsg;
    }

    // 4. 決定 Text
    let text = '';
    if (isSuccess) {
        text = response?.Data?.ReturnMsg || '';
    } else {
        text = '';
    }

    return {
        icon: alertIcon,
        title: title,
        text: text
    };
}

$('body').on('blur', 'input.websoft-positive-int', function () {
    if (!/^\d+$/.test($(this).val())) {
        $(this).val('1');
    }
});

function escapeHtml(v) {
    return $('<div>').text(v ?? '').html();
}