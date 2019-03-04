// Global var definitions


var IsPostBack=0;
var WFID_totRows;
var WFID_textABuscar;
var WFID_ResponseCode;
var WFID_ResponseMessage;
var contTot=-1;
var array = [];
var polo = [];
var statuslblWFID = $("#" + $("[id$='lblWorkflowStatus']").attr("id")).text();



$(document).ready(function () {
    preparaDivsWFIDs();
});

function JoinUrlParameters(i, data, enabled) {

    

    var urlFormatLink = "";
    var urlFormat ="";
    
    var dataSplit = data.split(':');

    urlFormat = RSUrl + "/Pages/Workflow.aspx?wfid=" + dataSplit[0] + "&wftype=" + dataSplit[1];
    urlFormatLink = '<a id="link_' + i + '" href="' + urlFormat + '" target="_blank">ID: ' + dataSplit[0] + '</a>';

    return urlFormatLink;
}

function generaListaWFID() {
    
    $("#" + WFID_Data).val(array.join("|"));
}

function WFID_deleteWFID(id) {
    debugger;
    var otro=[];
    var texto = $("#link_" + id).text();
    texto = texto.replace("ID: ", "");
    for (var i = 0; i < array.length; i++) {
        var datata=array[i].split(':')[0];
        if (datata!=texto) {
            otro.push(array[i]);
        }
    }
    array=[];
    array=otro;
    $("#" + WFID_Data).val(otro.join("|"));

    $("#row_" + id).remove();
    generaListaWFID();
}

function WFID_verifica(text) {
    
    var status = true;
    if ($.isNumeric(text)) {
        status = true;
    }
    else {
        status = false;
    }

    return status;
}

function preparaDivsWFIDs() {
    array = [];
    $('#WFID_Div').html('');
    var data = $("#" + WFID_Data).val().split("|");
    if (data!='') {
        for (i = 0; i < data.length; i++) {
            
            var statuslblWFID2 = $("#" + $("[id$='lblWorkflowStatus']").attr("id")).text();

            if (statuslblWFID2 == "Closed") {
                $('#WFID_Div').append('<div id="row_' + i + '" class="WFID_InternalLinkToWorkFlow">' + JoinUrlParameters(i, data[i]) + '</div>');
               // $("#" + $("[id$='WFID_buttonAdd']").attr("id")).attr('class', 'btn_blue_litle_LinkToWorkFlowDisabled');
            }
            else {
                var enabeled = $("#" + $("[id$='WFID_Textbox']").attr("id")).attr('disabled');
                if (enabeled) {
                    $('#WFID_Div').append('<div id="row_' + i + '" class="WFID_InternalLinkToWorkFlow">' + JoinUrlParameters(i, data[i]) + '</div>');
                    $("#" + $("[id$='WFID_buttonAdd']").attr("id")).attr('class', 'btn_blue_litle_LinkToWorkFlow');
                }
                else {
                    $('#WFID_Div').append('<div id="row_' + i + '" class="WFID_InternalLinkToWorkFlow">' + JoinUrlParameters(i, data[i]) + '<span class="xCloseEnabledLinkToWorkFlow" onclick="WFID_deleteWFID(' + i + ')"  id="close_' + i + '" >x</span><div>');
                    $("#" + $("[id$='WFID_buttonAdd']").attr("id")).attr('class', 'btn_blue_litle_LinkToWorkFlow');

                }
            }
            array.push(data[i]);
        }
        generaListaWFID();
    }
}

