define(["knockout", "toastr", "app/utils", "app/main"], function (
    ko,
    toastr,
    utils,
    webcli
) {
    function AddFileModel(params) {

        this.filename = ko.observable();

        this.submit = function () {
            var method = this.method();
            var isFileResult = this.isFileResult();
            if (!method) {
                toastr["error"]("Http method required.");
                return;
            }

            if (isFileResult) {
                if (document.getElementById("file").files.length == 0) {
                    toastr["error"]("Select a file.");
                    return;
                }
            }

            var endpoint = this.endpoint();
            if (!endpoint || (!isFileResult && method == "GET" && endpoint == "/")) {
                toastr["error"]("Endpoint invalid.");
                return;
            }

            var formData = new FormData();
            utils.objectToFormData(JSON.parse(), formData);

            var file = $("#file");
            if (file.length > 0) {
                var fileInput = file[0].files[0];
                formData.append("file", fileInput);
            }

            var ajaxOptions = {
                type: "POST",
                cache: false,
                contentType: "application/json",
                data: ko.toJSON(this),
                url: "/api/transform/savetransformfile",
                beforeSend: function () { },
                success: function (data, textStatus, jqXHR) {
                    toastr["success"]("Saved...");
                    webcli.refreshTree();
                },
                error: function (response) { }
            };

            var jqXHR = ($.ajax(ajaxOptions).always = function (
                data,
                textStatus,
                jqXHR
            ) { });
        }.bind(this);
    }

    AddFileModel.prototype.dispose = function () {
        // noop
    };

    return {
        viewModel: {
            createViewModel: function (params, componentInfo) {
                return new AddFileModel(params);
            }
        }
    };
});
