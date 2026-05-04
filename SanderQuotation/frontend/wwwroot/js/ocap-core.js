/**
 * ocap-core.js
 * Shared OCAP flow engine: jsPlumb init, node management,
 * endpoint handling, JSON import/export, info modal, JSON modal.
 *
 * Exposes global `OcapCore` used by page-specific inline scripts.
 */
var OcapCore = (function () {
    "use strict";

    /* ---- STATE ---- */
    var nodeCounter = 0;
    var nodes = {};
    var nodeEndpoints = {};
    var jp;
    var selectedId = null;
    var selectedConn = null;
    var isRunMode = false;
    var isDragging = false;
    var _edgeClickHandler = null;
    var _runNodeClickHandler = null;
    var _nodeSelectedCallback = null;
    var _skipDeselect = false;
    var canvas = null;
    var canvasWrap = null;

    /* ------------------------------------------------------------------ */
    /* ---- NODE DOM HELPERS -------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
    }

    /** Build innerHTML for a node element. */
    function nodeInnerHTML(type, label) {
        var text = escapeHtml(label);
        var btn = '<button class="node-info-btn" title="查看資訊">?</button>';
        if (type === "decision") {
            return '<div class="node-inner" style="position:relative;"><div class="diamond-box"><span class="node-text">' + text + '</span></div>' + btn + '</div>';
        }
        return '<div class="node-inner" style="position:relative;"><span class="node-text">' + text + '</span>' + btn + '</div>';
    }

    /** Create and return a detached node DOM element from nodeData. */
    function createNodeElement(nodeData) {
        var el = document.createElement("div");
        el.id = nodeData.id;
        el.className = "ocap-node node-" + nodeData.type;
        el.style.left = nodeData.x + "px";
        el.style.top = nodeData.y + "px";
        el.innerHTML = nodeInnerHTML(nodeData.type, nodeData.label);
        return el;
    }

    /** Attach click / info-btn listeners to an existing node element. */
    function bindNodeEvents(id, el) {
        el.addEventListener("click", function (e) {
            e.stopPropagation();
            if (isRunMode) return;
            selectNode(id);
        });
        el.querySelector(".node-info-btn").addEventListener("click", function (e) {
            e.stopPropagation();
            openInfoModal(id);
        });
        refreshInfoBtn(id);
    }

    /* ------------------------------------------------------------------ */
    /* ---- PANEL HELPERS ----------------------------------------------- */
    /* ------------------------------------------------------------------ */

    /**
     * Switch the right panel to one of three states:
     *   "none" – placeholder text
     *   "node" – node property fields
     *   "conn" – connection property fields
     */
    function showPanel(which) {
        var panelNone = document.getElementById("panel-none");
        var panelFields = document.getElementById("panel-fields");
        var panelConn = document.getElementById("panel-conn-fields");
        if (panelNone) panelNone.style.display = (which === "none") ? "block" : "none";
        if (panelFields) panelFields.style.display = (which === "node") ? "flex" : "none";
        if (panelConn) panelConn.style.display = (which === "conn") ? "flex" : "none";
    }

    function setInputVal(id, val) {
        var el = document.getElementById(id);
        if (el) el.value = val != null ? val : "";
    }

    /* ------------------------------------------------------------------ */
    /* ---- INIT --------------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function init(canvasId, canvasWrapId, runMode, readyFn) {
        canvas = document.getElementById(canvasId);
        canvasWrap = document.getElementById(canvasWrapId);
        isRunMode = !!runMode;

        jsPlumb.ready(function () {
            jp = jsPlumb.getInstance({
                Container: canvasId,
                Connector: ["Flowchart", { cornerRadius: 8, stub: 30 }],
                PaintStyle: { stroke: "#444c56", strokeWidth: 2 },
                HoverPaintStyle: { stroke: "#58a6ff", strokeWidth: 2.5 },
                EndpointStyle: { fill: "#444c56", stroke: "#58a6ff", strokeWidth: 1.5, radius: 5 },
                EndpointHoverStyle: { fill: "#58a6ff" },
                Overlays: [["Arrow", { width: 10, length: 10, location: 1, id: "arrow" }]],
                ConnectionsDetachable: false,
            });

            /* -- Connection created: hide endpoints, init label, bind click -- */
            jp.bind("connection", function (info) {
                var conn = info.connection;
                conn.label = "";
                [conn.endpoints[0], conn.endpoints[1]].forEach(function (ep) {
                    if (ep && ep.canvas) {
                        ep.canvas.style.opacity = "0";
                        ep.canvas.style.pointerEvents = "none";
                    }
                });
                /* Attach click directly to connector SVG canvas.
                   Using jp.bind("click") risks container-level capture delegation
                   in jsPlumb 2.x which can block the run-mode node-click handler. */
                if (conn.canvas) {
                    conn.canvas.style.cursor = "pointer";
                    conn.canvas.addEventListener("click", function (e) {
                        e.stopPropagation();
                        _skipDeselect = true;
                        if (isRunMode) {
                            if (_edgeClickHandler) _edgeClickHandler(conn);
                        } else {
                            selectNode(null);
                            selectConn(conn);
                        }
                    });
                }
            });

            jp.bind("connectionDrag", function () { isDragging = true; });
            jp.bind("connectionDragStop", function () {
                isDragging = false;
                hideAllEndpoints();
            });

            /* -- Canvas background click: deselect -- */
            canvas.addEventListener("click", function () {
                if (_skipDeselect) { _skipDeselect = false; return; }
                if (!isRunMode) {
                    selectNode(null);
                    selectConn(null);
                }
            });

            /* -- Run-mode node click (capture phase) -- */
            canvas.addEventListener("click", function (e) {
                if (!isRunMode) return;
                if (e.target.closest(".node-info-btn")) return;
                var nodeEl = e.target.closest(".ocap-node");
                if (nodeEl && _runNodeClickHandler) _runNodeClickHandler(nodeEl.id);
            }, true);

            if (readyFn) readyFn();
        });
    }

    function hideAllEndpoints() {
        Object.values(nodeEndpoints).forEach(function (eps) {
            eps.forEach(function (ep) {
                if (ep.canvas) {
                    ep.canvas.style.opacity = "0";
                    ep.canvas.style.pointerEvents = "none";
                }
            });
        });
    }

    /* ------------------------------------------------------------------ */
    /* ---- ADD NODE ----------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function addNode(type, label, x, y) {
        var id = "node_" + (++nodeCounter);
        var nodeData = { id: id, type: type, label: label, desc: "", x: x, y: y };
        nodes[id] = nodeData;

        var el = createNodeElement(nodeData);
        canvas.appendChild(el);

        if (!isRunMode) {
            jp.draggable(el, {
                containment: false,
                stop: function () {
                    nodes[id].x = parseInt(el.style.left);
                    nodes[id].y = parseInt(el.style.top);
                }
            });
        }

        nodeEndpoints[id] = makeNodeEndpoints(id, el);
        bindNodeEvents(id, el);
        return id;
    }

    /* ------------------------------------------------------------------ */
    /* ---- ENDPOINTS ---------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function makeNodeEndpoints(id, el) {
        var eps = [];
        var hideTimer = null;

        function setEpVis(ep, visible) {
            if (!ep.canvas) return;
            ep.canvas.style.opacity = visible ? "1" : "0";
            ep.canvas.style.pointerEvents = visible ? "all" : "none";
        }

        function showEps() {
            if (hideTimer) { clearTimeout(hideTimer); hideTimer = null; }
            if (!isRunMode) eps.forEach(function (ep) { setEpVis(ep, true); });
        }

        function scheduleHide() {
            hideTimer = setTimeout(function () {
                if (isDragging) return;
                eps.forEach(function (ep) { setEpVis(ep, false); });
                hideTimer = null;
            }, 120);
        }

        jp.makeTarget(el, {
            anchor: "Continuous",
            allowLoopback: false,
            maxConnections: -1,
            endpoint: ["Dot", { radius: 5 }],
            dropOptions: { hoverClass: "node-drop-hover" }
        });

        ["Top", "Right", "Bottom", "Left"].forEach(function (anchor) {
            var ep = jp.addEndpoint(el, {
                anchor: anchor,
                isSource: true,
                isTarget: true,
                endpoint: ["Dot", { radius: 7 }],
                paintStyle: { fill: "#0d1117", stroke: "#58a6ff", strokeWidth: 2 },
                hoverPaintStyle: { fill: "#58a6ff", stroke: "#58a6ff" },
                connectorStyle: { stroke: "#444c56", strokeWidth: 2 },
                connectorHoverStyle: { stroke: "#58a6ff", strokeWidth: 2.5 },
                connector: ["Flowchart", { cornerRadius: 8, stub: 30 }],
                overlays: [["Arrow", { width: 10, length: 10, location: 1, id: "arrow" }]],
                maxConnections: -1,
                allowLoopback: false,
            });
            if (ep.canvas) {
                ep.canvas.style.opacity = "0";
                ep.canvas.style.pointerEvents = "none";
            }
            eps.push(ep);
        });

        eps.forEach(function (ep) {
            if (ep.canvas) {
                ep.canvas.addEventListener("mouseenter", showEps);
                ep.canvas.addEventListener("mouseleave", scheduleHide);
            }
        });

        el.addEventListener("mouseenter", showEps);
        el.addEventListener("mouseleave", scheduleHide);

        return eps;
    }

    /* ------------------------------------------------------------------ */
    /* ---- SELECT NODE / CONN ------------------------------------------ */
    /* ------------------------------------------------------------------ */

    function selectNode(id) {
        /* Deselect active connection */
        if (selectedConn) {
            if (selectedConn.canvas) selectedConn.canvas.classList.remove("conn-selected");
            selectedConn = null;
            showPanel("none");
        }
        /* Deselect previous node */
        if (selectedId) {
            var prev = document.getElementById(selectedId);
            if (prev) prev.classList.remove("selected");
        }
        selectedId = id;
        if (id && nodes[id]) {
            var n = nodes[id];
            setInputVal("prop-id", n.id);
            setInputVal("prop-label", n.label);
            setInputVal("prop-desc", n.desc);
            setInputVal("prop-type", n.type);
            document.getElementById(id).classList.add("selected");
            showPanel("node");
        } else {
            showPanel("none");
        }
        // Sync image preview in design panel
        var imgPreview = document.getElementById("prop-img-preview");
        var imgClearBtn = document.getElementById("btn-clear-image");
        var imgFileInput = document.getElementById("prop-image-file");
        if (imgPreview) {
            var hasImg = id && nodes[id] && nodes[id].image;
            imgPreview.src = hasImg ? nodes[id].image : "";
            imgPreview.style.display = hasImg ? "block" : "none";
            if (imgClearBtn) imgClearBtn.style.display = hasImg ? "flex" : "none";
        }
        if (imgFileInput) imgFileInput.value = "";
        if (_nodeSelectedCallback) _nodeSelectedCallback(id);
    }

    function selectConn(conn) {
        if (selectedConn) {
            if (selectedConn.canvas) selectedConn.canvas.classList.remove("conn-selected");
        }
        selectedConn = conn;
        if (conn) {
            if (conn.canvas) conn.canvas.classList.add("conn-selected");
            var lbl = conn.getOverlay ? conn.getOverlay("lbl") : null;
            setInputVal("prop-conn-label", lbl ? lbl.getLabel() : (conn.label || ""));
            showPanel("conn");
        } else {
            if (!selectedId) showPanel("none");
        }
    }

    /* ------------------------------------------------------------------ */
    /* ---- INFO BUTTON -------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function refreshInfoBtn(id) {
        var el = document.getElementById(id);
        if (!el) return;
        var btn = el.querySelector(".node-info-btn");
        if (!btn) return;
        var n = nodes[id];
        btn.style.display = (isRunMode && n && (n.desc || n.image)) ? "block" : "none";
    }

    function refreshAllInfoBtns() {
        Object.keys(nodes).forEach(refreshInfoBtn);
    }

    function openInfoModal(id) {
        var n = nodes[id];
        if (!n) return;
        var typeMap = { start: "開始", end: "結束", action: "動作", decision: "決策" };
        document.getElementById("info-modal-title").textContent = n.label;
        document.getElementById("info-modal-type").textContent = typeMap[n.type] || n.type;
        var body = document.getElementById("info-modal-body");
        var html = "";
        if (n.image) {
            html += '<img src="' + n.image + '" class="ocap-info-modal-img" alt="節點圖片">';
        }
        html += '<div class="ocap-info-modal-desc">' + escapeHtml(n.desc || "(無說明內容)") + '</div>';
        body.innerHTML = html;
        bootstrap.Modal.getOrCreateInstance(document.getElementById("info-modal")).show();
    }

    /* ------------------------------------------------------------------ */
    /* ---- EXPORT ------------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function exportFlow() {
        var edges = jp.getAllConnections().map(function (c) {
            var srcEps = nodeEndpoints[c.sourceId] || [];
            var tgtEps = nodeEndpoints[c.targetId] || [];
            return {
                id: c.id,
                source: c.sourceId,
                target: c.targetId,
                label: c.label || getConnectionLabel(c),
                sourceEpIdx: srcEps.indexOf(c.endpoints[0]),
                targetEpIdx: tgtEps.indexOf(c.endpoints[1]),
            };
        });
        return { nodes: Object.values(nodes), edges: edges };
    }

    function getConnectionLabel(c) {
        var lbl = c.getOverlay("lbl");
        return lbl ? lbl.getLabel() : "";
    }

    /* ------------------------------------------------------------------ */
    /* ---- IMPORT ------------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function importFlow(json) {
        try {
            var data = typeof json === "string" ? JSON.parse(json) : json;

            jp.deleteEveryEndpoint();
            Object.keys(nodes).forEach(function (id) {
                var el = document.getElementById(id);
                if (el) el.remove();
            });
            nodes = {};
            nodeEndpoints = {};
            nodeCounter = 0;

            data.nodes.forEach(function (n) {
                nodeCounter = Math.max(nodeCounter, parseInt(n.id.replace("node_", "")) || 0);
                nodes[n.id] = Object.assign({}, n);

                var el = createNodeElement(n);
                canvas.appendChild(el);

                if (!isRunMode) {
                    jp.draggable(el, {
                        containment: false,
                        stop: function () {
                            nodes[n.id].x = parseInt(el.style.left);
                            nodes[n.id].y = parseInt(el.style.top);
                        }
                    });
                }

                nodeEndpoints[n.id] = makeNodeEndpoints(n.id, el);
                bindNodeEvents(n.id, el);
            });

            setTimeout(function () {
                data.edges.forEach(function (edge) {
                    var srcEps = nodeEndpoints[edge.source] || [];
                    var tgtEps = nodeEndpoints[edge.target] || [];
                    var sourceEp = (edge.sourceEpIdx >= 0 && srcEps[edge.sourceEpIdx]) ? srcEps[edge.sourceEpIdx] : edge.source;
                    var targetEp = (edge.targetEpIdx >= 0 && tgtEps[edge.targetEpIdx]) ? tgtEps[edge.targetEpIdx] : edge.target;
                    var conn = jp.connect({ source: sourceEp, target: targetEp });
                    if (conn && edge.label) {
                        conn.addOverlay(["Label", { label: edge.label, id: "lbl", cssClass: "jtk-overlay", location: 0.5 }]);
                        conn.label = edge.label;
                    }
                });
                refreshAllInfoBtns();
            }, 100);

            selectNode(null);
        } catch (e) {
            alert("JSON 格式錯誤: " + e.message);
        }
    }

    /* ------------------------------------------------------------------ */
    /* ---- JSON MODAL --------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    var modalIsImport = false;

    function openJsonModal(title, content, isImport) {
        modalIsImport = isImport;
        document.getElementById("json-modal-title").textContent = title;
        document.getElementById("json-textarea").value = content;
        var okBtn = document.getElementById("btn-modal-ok");
        if (okBtn) okBtn.style.display = isImport ? "" : "none";
        var modalEl = document.getElementById("json-modal");
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
        if (isImport) {
            modalEl.addEventListener("shown.bs.modal", function once() {
                document.getElementById("json-textarea").focus();
                modalEl.removeEventListener("shown.bs.modal", once);
            });
        }
    }

    function closeJsonModal() {
        bootstrap.Modal.getOrCreateInstance(document.getElementById("json-modal")).hide();
    }

    var _importDoneCallback = null;

    function bindJsonModal(importDoneFn) {
        if (importDoneFn) _importDoneCallback = importDoneFn;
        var okBtn = document.getElementById("btn-modal-ok");
        if (!okBtn) return;
        okBtn.addEventListener("click", function () {
            if (modalIsImport) {
                importFlow(document.getElementById("json-textarea").value);
                closeJsonModal();
                setTimeout(function () {
                    if (_importDoneCallback) _importDoneCallback();
                }, 200);
            } else {
                closeJsonModal();
            }
        });
    }

    /* ------------------------------------------------------------------ */
    /* ---- CLEAR / REMOVE ---------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function clearAll() {
        jp.deleteEveryEndpoint();
        Object.keys(nodes).forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.remove();
        });
        nodes = {};
        nodeEndpoints = {};
        selectNode(null);
    }

    function removeNode(id) {
        jp.remove(id);
        delete nodes[id];
        delete nodeEndpoints[id];
    }

    function deleteSelectedConn() {
        if (!selectedConn) return;
        jp.deleteConnection(selectedConn);
        selectedConn = null;
        showPanel("none");
    }

    /* ------------------------------------------------------------------ */
    /* ---- RUN MODE ----------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    function setRunMode(val) {
        isRunMode = val;
        jp.setDraggable(Object.keys(nodes), !isRunMode);
        if (isRunMode) hideAllEndpoints();
        refreshAllInfoBtns();
    }

    /* ------------------------------------------------------------------ */
    /* ---- PUBLIC API --------------------------------------------------- */
    /* ------------------------------------------------------------------ */

    return {
        init: init,
        addNode: addNode,
        selectNode: selectNode,
        selectConn: selectConn,
        clearAll: clearAll,
        removeNode: removeNode,
        deleteSelectedConn: deleteSelectedConn,
        exportFlow: exportFlow,
        importFlow: importFlow,
        openJsonModal: openJsonModal,
        bindJsonModal: bindJsonModal,
        refreshAllInfoBtns: refreshAllInfoBtns,
        refreshInfoBtn: refreshInfoBtn,
        setEdgeClickHandler: function (fn) { _edgeClickHandler = fn; },
        setRunNodeClickHandler: function (fn) { _runNodeClickHandler = fn; },
        setNodeSelectedCallback: function (fn) { _nodeSelectedCallback = fn; },
        setRunMode: setRunMode,
        getJp: function () { return jp; },
        getNodes: function () { return nodes; },
        getNodeEndpoints: function () { return nodeEndpoints; },
        getSelectedId: function () { return selectedId; },
        getSelectedConn: function () { return selectedConn; },
        isInRunMode: function () { return isRunMode; },
        getCanvas: function () { return canvas; },
        getCanvasWrap: function () { return canvasWrap; },
    };
})();