function ReportViewer(apiPath, rdlName) {

    var self = this;

    self.toggleItemRequests = [];
    self.metadata = [];
    self.apiPath = apiPath;
    self.rdlName = rdlName;

    function constructReportParameters() {
        var eles = $('.report-viewer input');
        var dtoArr = [];
        var keys = [];

        for (var i = 0; i < eles.length; i++) {
            var ele = $(eles[i]);

            var eleId = ele.attr('id');
            var eleName = ele.attr('name');
            var multiValue = ele.data('multivalue');
            var nullable = ele.data('nullable');
            var eleType = ele.attr('type');
            var dataType = ele.data('datatype');
            var dto = null;

            if (keys.indexOf(eleName) === -1) {
                dto = {
                    Name: eleName
                };
            }
            else {
                var idx = keys.indexOf(eleName);
                dto = dtoArr[idx];
            }

            if (eleType === 'checkbox') {
                if (multiValue) {
                    if (!dto.Values) {
                        dto.Values = [];
                    }
                    
                    if (ele.is(':checked')) {
                        dto.Values.push(ele.val());
                    }
                }
                else {
                    if (ele.is(':checked')) {
                        dto.Value = ele.val();
                    }
                    else if (dataType && dataType === 'boolean') {
                        dto.Value = 'false';
                    }
                }
            }
            else {
                dto.Value = ele.val();
            }

            if (keys.indexOf(eleName) === -1) {
                dtoArr.push(dto);
                keys.push(eleName);
            }
        }

        return dtoArr;
    }

    self.selectCheckbox = function (element, selectAll, deselectAll) {
        var id = $(element).attr('id');
        var list = id.indexOf('-') > -1;

        var multivalue = $(element).data('multivalue');
        var checked = $(element).is(':checked');

        if (list) {
            var idSplit = id.split('-')[0];
            var eles = $('input[id*="' + idSplit + '"]');

            if (!multivalue || multivalue === 'false') {
                $(eles).prop('checked', false);
            }
        }

        if (selectAll) {
            $(element).prop('checked', true);
        }
        else if (deselectAll) {
            $(element).prop('checked', false);
        }
        else {
            $(element).prop('checked', checked);
        }        
    }
        
    self.postReportParameters = function (callback) {
        var dtoArr = constructReportParameters();
        var dto = {
            Parameters: dtoArr            
        };

        return $.ajax({
            method: 'POST',
            url: `${self.apiPath.replace(/\/$/, "")}/GenerateParameters?rdl=${self.rdlName}`,
            data: JSON.stringify(dto),
            contentType: 'application/json; charset=utf-8'
        }).done(function (data, textStatus, jqXHR) {
            $('.report-viewer').html(data.value);

            let paramLists = $('.report-viewer .reportparam-list');

            if (callback) {
                callback();
            }

            $('.report-viewer .reportparameters-container').on("click", function (e) {                
                if (e.target !== e.currentTarget) {
                    return;
                }

                self.processUserAction();
            });

            $('.report-viewer .reportparam-list-select .over-select').on("click", function (e) {
                e.preventDefault();

                self.processUserAction(function () {
                    let currentTarget = e.currentTarget;
                    let dropdownContainer = $(currentTarget).closest('.reportparam-list').find('.reportparam-list-dropdown');
                    let dropdownOptions = $(dropdownContainer).find('.custom-checkbox');

                    if ($(dropdownContainer).css('display') === 'none' && dropdownOptions.length > 0) {
                        $(dropdownContainer).addClass('open');
                        $(dropdownContainer).css('display', 'block');
                    }
                });                       
            });

            $('.report-viewer .reportparam-list-dropdown .reportparam-list-selectall').on("click", function () {
                let dropdownContainer = $(this).closest('.reportparam-list').find('.reportparam-list-dropdown');
                let checkboxes = $(dropdownContainer).find('input[type="checkbox"]');

                $.each(checkboxes, function (idx, ele) {
                    self.selectCheckbox($(ele), true, false);
                });
            });

            $('.report-viewer .reportparam-list-dropdown .reportparam-list-deselectall').on("click", function () {
                let dropdownContainer = $(this).closest('.reportparam-list').find('.reportparam-list-dropdown');
                let checkboxes = $(dropdownContainer).find('input[type="checkbox"]');

                $.each(checkboxes, function (idx, ele) {
                    self.selectCheckbox($(ele), false, true);
                });
            });

            $('.report-viewer .custom-control-input[data-datatype="boolean"]').on("click", function () {
                if ($(this).data('requiredparam')) {
                    self.postReportParameters();
                }
            });

            $('.report-viewer input[type="text"]').on("focus", function (event) {
                self.processUserAction();
            });

            $('.report-viewer input[type="checkbox"]').on("change", function (event) {
                self.selectCheckbox($(this), false, false);
            });

            $('.report-viewer #RunReportBtn').on('click', function () {
                self.renderReport();
            });
        }).fail(function (jqXHR, textStatus, errorThrown) {
            console.error(errorThrown);
        });
    }

    self.processUserAction = function (callback) {
        let paramLists = $('.report-viewer .reportparam-list');
        let load = false;

        $.each(paramLists, function (idx, list) {
            let dropdownContainers = $(list).find('.reportparam-list-dropdown[class*="open"]');

            $.each(dropdownContainers, function (idx, ele) {
                $(ele).removeClass('open');
                $(ele).css('display', 'none');
                load = true;
            });
        });

        if (load || paramLists.length === 0) {
            self.postReportParameters(callback);
        } else if (callback) {
            callback();
        }
    };

    self.renderReport = function() {
        var dtoArr = constructReportParameters();
        var dto = {
            Parameters: dtoArr,
            ToggleItemRequests: self.toggleItemRequests,
            Metadata: self.metadata
        };
        
        return $.ajax({
            method: 'POST',
            url: `${self.apiPath.replace(/\/$/, "")}/GenerateReport?rdl=${self.rdlName}`,
            data: JSON.stringify(dto),
            contentType: 'application/json; charset=utf-8'
        }).done(function (data, textStatus, jqXHR) {
            $('.reportoutput-container').html(data.value);

            $('button[data-toggler="true"]').on('click', function () {

                var togglerName = $(this).data('toggler-name').toString();
                var nameIdx = self.toggleItemRequests.indexOf(togglerName);

                if (nameIdx > -1) {
                    self.toggleItemRequests.splice(nameIdx, 1);
                }
                else {
                    self.toggleItemRequests.push(togglerName);
                }

                self.renderReport();
            });

            $('.report-viewer .reportviewer-table-pager button[data-direction="prev"]').on('click', function (event) {
                self.changePage($(this).data('tablename'), 'prev');
            });

            $('.report-viewer .reportviewer-table-pager button[data-direction="next"]').on('click', function (event) {
                self.changePage($(this).data('tablename'), 'next');
            });
        }).fail(function (jqXHR, textStatus, errorThrown) {
            console.error(errorThrown);
        });
    }

    self.changePage = function (name, dir) {
        const idx = self.metadata.findIndex((obj) => {
            return obj.Key === 'key_tablixpage' && obj.ObjectName === name;
        });

        if (idx > -1) {
            self.metadata[idx].Value = dir === 'prev' ? (parseInt(self.metadata[idx].Value) - 1).toString() : (parseInt(self.metadata[idx].Value) + 1).toString()
        }
        else {
            // Table starts at page 1.            
            self.metadata.push({
                Key: 'key_tablixpage',
                ObjectName: name,
                Value: dir === 'prev' ? '1' : '2'
            });
        }

        self.renderReport();
    }

    return {        
        postReportParameters: self.postReportParameters,
        renderReport: self.renderReport
    }
}