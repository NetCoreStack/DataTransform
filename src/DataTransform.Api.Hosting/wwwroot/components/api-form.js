define(["knockout", "toastr", "app/utils", "app/main"], function (
    ko,
    toastr,
    utils,
    webcli
) {
    function AddFileModel(params) {
        var self = this;

        var originFilename = "";
        if (params && params.context) {
            originFilename = params.context().originFilename;
        }
        
        this.filename = ko.observable(originFilename);

        this.submit = function () {

            var ajaxOptions = {
                type: "POST",
                cache: false,
                contentType: "application/json",
                data: JSON.stringify({ originFilename: originFilename, filename: self.filename() }),
                url: "/api/transform/createtransformfile",
                success: function (data, textStatus, jqXHR) {
                    toastr["success"]("Saved...");
                    webcli.refreshTree();
                },
                error: function (response) {
                    toastr["error"](response.responseText);
                }
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
                console.log("CREATE VIEW MODEL", params.context());
                return new AddFileModel(params);
            }
        }
    };
});
