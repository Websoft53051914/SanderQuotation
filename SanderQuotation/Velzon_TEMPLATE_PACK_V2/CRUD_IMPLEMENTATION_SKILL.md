---
name: velzon-crud-implementation-skill
description: 依 EIP.frontend 的 Velzon 既有版型與通用範本，快速產生 CRUD 畫面與對應 Controller。
---

# Velzon CRUD Implementation Skill

## Role

You must follow the existing Velzon visual/layout pattern in this repository.

## Required Input

Expect these requirement blocks from user input:

1. 功能名稱
2. 查詢條件
3. 列表欄位
4. 編輯欄位

欄位規格建議格式：

-   欄位名稱｜型態
-   欄位名稱｜型態｜選項
-   欄位名稱｜型態｜必填
-   欄位名稱｜型態｜選項｜必填
-   只有必填才需要標註 `必填`，未標註一律視為非必填
-   若欄位型態不需要選項，可直接省略該段，不需要保留空欄

Parsing tolerance rules:

-   Token 順序可容錯，不強制固定位置。
-   只要有辨識到 `必填` 就視為 required。
-   先辨識欄位型態（text/number/textarea/select/radio/checkbox/checkbox-group/datepicker/file）。
-   其餘且含 `/` 或多值描述的片段，優先視為選項內容。

If business rules are missing, keep structure complete and mark unknown logic as TODO.

## Read-First Files

Before generating code, read these files in order:

1. `Velzon_TEMPLATE_PACK/References/FeatureController.txt`
2. `Velzon_TEMPLATE_PACK/References/Index.txt`

If user gives additional reference pages, prioritize them while preserving this skill constraints.

## Non-Negotiable Rules

1. Keep Velzon style (card header, search row, table card, pagination, modal).
2. Always use single-page CRUD (`Index.cshtml` + modal form).
3. Select fields use `Choices.js` initialization style (same as project pattern).
4. Date fields use `flatpickr` pattern; fallback to native `datetime-local` only if needed.
5. Search/list state must be managed by front-end query state object.
6. For demo mode, data source must be hardcoded in front-end and labels in Chinese.
7. File upload defaults to native html `input type="file"` (single file), unless user asks multiple files.
8. Do not generate standalone `Edit.cshtml`, `Create.cshtml`, or split-page edit flow.
9. Do not invent backend business rules when input does not provide them.
10. Required rendering rule: only mark required fields when input explicitly contains `必填`.
11. If a field is required, render both label required marker and HTML validation:
    - Add `required` class on label (or equivalent required marker pattern in this project).
    - Add HTML `required` attribute on native form controls when applicable.
12. If `必填` marker is omitted, treat field as optional and do not add required marker/attribute.

## Field Type Mapping

1. `text` -> text input
2. `number` -> numeric input
3. `textarea` -> multiline input
4. `select` -> dropdown (Choices.js)
5. `radio` -> single choice group
6. `checkbox` -> single boolean
7. `checkbox-group` -> multiple selection checkboxes
8. `datepicker` -> date/datetime picker
9. `file` -> file upload input

### Required Behavior by Field Type

1. `text` / `number` / `textarea` / `select` / `datepicker` / `file`
    - Required: label required marker + HTML `required`
    - Optional: no label required marker + no HTML `required`
2. `radio`
    - Required: label required marker + group-level required validation
    - Optional: no required marker + no required validation
3. `checkbox`
    - Required: label required marker + checkbox required validation
    - Optional: no required marker + no required validation
4. `checkbox-group`
    - Required: label required marker + custom validation (at least one checked)
    - Optional: no required marker + skip at-least-one validation

## Output Contract

When implementing a new feature, produce:

1. Controller (at least `Index` action)
2. `Views/EIP/<FeatureName>/Index.cshtml`
3. Modal-based create/edit UI inside `Index.cshtml`
4. Required ViewModel updates only when backend binding is required

## Quality Gate

Before finalizing:

1. Verify Velzon layout structure is preserved.
2. Verify required field types are rendered correctly.
3. Verify no unsupported UI pattern is introduced.
4. Verify compile/syntax errors are resolved.
5. Verify unresolved requirements are explicitly marked TODO.
