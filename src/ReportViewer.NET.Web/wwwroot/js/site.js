﻿function ReportViewer() {

    var self = this;

    self.toggleItemRequests = [];

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

    self.postReportParameters = function () {
        var dtoArr = constructReportParameters();
        var dto = {
            Parameters: dtoArr            
        };

        return $.ajax({
            method: 'POST',
            url: `/Home/ParameterViewer`,
            data: JSON.stringify(dto),
            contentType: 'application/json; charset=utf-8'
        }).done(function (data, textStatus, jqXHR) {
            $('.report-viewer').html(data.value);

            $('.report-viewer input:not(.custom-control-input)').on("focusout", function () {
                if ($(this).data('requiredparam')) {
                    self.postReportParameters();
                }
            });

            $('.report-viewer .custom-control-input').on("click", function () {
                if ($(this).data('requiredparam')) {
                    self.postReportParameters();
                }
            });

            $('.report-viewer input[type="checkbox"]').on("change", function (event) {
                var id = $(this).attr('id');
                var list = id.indexOf('-') > -1;

                var multivalue = $(this).data('multivalue');
                var checked = this.checked;

                if (list) {
                    var idSplit = id.split('-')[0];
                    var eles = $('input[id*="' + idSplit + '"]');

                    if (!multivalue || multivalue === 'false') {
                        $(eles).prop('checked', false);
                    }
                }

                $(this).prop('checked', checked);
            });

            $('.report-viewer #RunReportBtn').on('click', function () {
                self.renderReport();
            });
        }).fail(function (jqXHR, textStatus, errorThrown) {
            console.error(errorThrown);
        });
    }

    self.renderReport = function() {
        var dtoArr = constructReportParameters();
        var dto = {
            Parameters: dtoArr,
            ToggleItemRequests: self.toggleItemRequests
        };
        
        return $.ajax({
            method: 'POST',
            url: `/Home/ReportViewer`,
            data: JSON.stringify(dto),
            contentType: 'application/json; charset=utf-8'
        }).done(function (data, textStatus, jqXHR) {
            $('.reportoutput-container').html(data.value);

            $('button[data-toggler="true"]').on('click', function () {
                
                var togglerName = $(this).data('toggler-name');
                var nameIdx = self.toggleItemRequests.indexOf(togglerName);

                if (nameIdx > -1) {
                    self.toggleItemRequests.splice(nameIdx, 1);
                }
                else {
                    self.toggleItemRequests.push(togglerName);
                }

                self.renderReport();
            })
        }).fail(function (jqXHR, textStatus, errorThrown) {
            console.error(errorThrown);
        });
    }

    return {        
        postReportParameters: self.postReportParameters,
        renderReport: self.renderReport
    }
}