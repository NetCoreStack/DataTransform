define(
    [
        "jquery",
        "bootstrap",
        "jsoneditor",
        "keymaster",
        "knockout",
        "knockout-jsoneditor",
        "websocket",
        "toastr",
        "logger",
        "app/utils",
        "app/main"
    ],
    function (
        $,
        bootstrap,
        jsoneditor,
        key,
        ko,
        editor,
        websocket,
        toastr,
        logger,
        utils,
        webcli
    ) {
        ko.components.register("empty", {
            template: "<div></div>"
        });

        ko.components.register("api-form", {
            viewModel: { require: "components/api-form" },
            template: { require: "text!components/api-form.html" }
        });

        ko.components.register("modal", {
            viewModel: { require: "components/modal" },
            template: { require: "text!components/modal.html" }
        });

        ko.bindingHandlers.scrollTo = {
            update: function (element, valueAccessor, allBindings) {
                var _value = valueAccessor();
                var _valueUnwrapped = ko.unwrap(_value);
                if (_valueUnwrapped) {
                    element.scrollIntoView();
                }
            }
        };

        function PageViewModel() {
            var self = this;
            self.filename = ko.observable();
            self.content = ko.observable();
            self.port = ko.observable(window.location.port);
            self.selectedTreeNode = ko.observable();
            self.consoleLogger = ko.observableArray();
            self.progress = ko.observable();
            self.readyForTask = ko.observable(true);
            self.showModal = ko.observable(false);
            self.modalMode = ko.observable("create");
            self.modalComponentName = ko.observable("empty");
            self.modalComponentTitle = ko.observable("");
            self.modalContext = ko.computed(function () {
                var filename = self.selectedTreeNode();
                if (self.modalMode() == "create") {
                    filename = "";
                }
                return {
                    originFilename: filename
                };
            }, this);
            self.createTransformFile = function () {
                self.modalMode("create");
                self.showModalDialog("api-form", "Create Transform File");
            };
            self.renameTransformFile = function () {
                self.modalMode("edit");
                self.showModalDialog("api-form", "Rename Transform File");
            };
            self.deleteTransformFile = function () {
                var $toast = toastr["error"](
                    "Are you sure you want to delete the " +
                    self.file() +
                    "<br/>" +
                    ' <button class="btn btn-danger btn-sm" id="deleteBtn">YES</button>',
                    "Delete API",
                    { closeButton: true }
                );

                if ($toast.find("#deleteBtn").length) {
                    $toast.delegate("#deleteBtn", "click", function () {
                        var type = self.type();
                        var endpoint = self.endpoint();
                        var url =
                            config.deletePath +
                            "?endpoint=" +
                            encodeURIComponent(endpoint) +
                            "&method=" +
                            type;
                        $.ajax({
                            type: "GET",
                            cache: false,
                            url: url,
                            success: function (response) {
                                webcli.refreshTree();
                            }
                        });
                    });
                }
            },
            self.pageTitle = ko.computed(function () {
                return "NetCoreStack DataTransform:" + this.port();
                }, this);
            self.showModalDialog = function (componentName, title) {
                self.modalComponentName(componentName);
                self.modalComponentTitle(title);
                self.showModal(true);
            };
            self.run = function (vm, sender) {
                self.readyForTask(false);
                var jqXHR = ($.ajax({
                    type: "GET",
                    cache: false,
                    url: "/api/transform/starttransformasync?filename=" + self.selectedTreeNode(),
                    success: function (data, textStatus, jqXHR) { },
                    error: function (response) { }
                }).always = function (data, textStatus, jqXHR) {

                });
            },
            self.saving = ko.computed(function () {
                if (self.progress()) {
                    var p = "bust=" + new Date().getTime();
                    return (
                        '<span class="span-status">Saving&nbsp;</span><img src="/img/auto_saving.gif?' +
                        p +
                        '" />'
                    );
                }
            });
            self.save = function () {
                try {
                    JSON.parse(self.content());
                } catch (e) {
                    var $toast = toastr["error"](e, { closeButton: true });
                    return false;
                }
                var jqXHR = ($.ajax({
                    type: "POST",
                    cache: false,
                    url: "/api/transform/saveconfig",
                    data: JSON.stringify({
                        configFileName: self.selectedTreeNode(),
                        content: self.content()
                    }),
                    contentType: "application/json; charset=utf-8",
                    beforeSend: function () {
                        self.progress(true);
                    },
                    success: function (data, textStatus, jqXHR) { },
                    error: function (response) { }
                }).always = function (data, textStatus, jqXHR) {
                    setTimeout(function () {
                        self.progress(false);
                    }, 1200);
                });
            };
            self.refreshTree = function () {
                $jsTree.jstree(true).refresh();
            };
            self.showModal.subscribe(function (newValue) {
                if (!newValue) {
                    self.modalComponentName("empty");
                    self.modalComponentTitle("");
                }
            });
        }

        var vm = new PageViewModel();
        ko.applyBindings(vm);

        var currentWsUrl =
            "ws://" +
            location.hostname +
            ":" +
            vm.port() +
            "/ws?connectionId=" +
            new Date().getTime();

        var WebSocketCommands = {
            Connect: 1,
            DataSend: 2,
            Handshake: 4,
            All: 7
        }

        var ws = new websocket(currentWsUrl);

        send = function (data) {
            ws.send(data);
        };

        ws.onmessage = function (msg) {
            var payload = JSON.parse(msg.data);
            if (payload.Command !== 4) {
                // var $toast = toastr[payload.Value.resultState](payload.Value.message, { closeButton: true, positionClass: "toast-bottom-right" });
                var logColor = undefined;
                if (payload.Value.resultState == "error") {
                    logColor = "#ff3e3e";
                }

                if (payload.Value.resultState == "completed") {
                    vm.readyForTask(true);
                }

                Logger.print(payload.Value.message, logColor);
            }
        };

        ws.onopen = function (context) {
            console.log("Socket open:", context);
        };

        window.viewModel = vm;
        document.title = vm.pageTitle();

        webcli.subscribe(webcli.events.treeChanged, function (sender, context) {

            var id = context.id;
            var type = context.type;

            vm.selectedTreeNode(id);
            Logger.print("Selected configuration file: " + vm.selectedTreeNode());

            var url = "/api/transform/getcontent" + "?filename=" + encodeURIComponent(id);

            $.getJSON(url, function (response) {
                if (response && response.data) {
                    vm.content(response.data);
                } else {
                    vm.content("");
                }
            });
        });

        window.consoleWs = ws;

        Logger.toggle();
        Logger.print("Window is loaded.");
    }
);
