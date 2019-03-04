// Global var definitions

var array = [];

$(document).ready(function () {
    paintLinkToWFIDs();
});

function JoinUrlParameters(i, data, enabled) {    
    var urlFormatLink = "";
    var urlFormat ="";
    
    var dataSplit = data.split(':');

    urlFormat = RSUrl + "/Pages/Workflow.aspx?wfid=" + dataSplit[0] + "&wftype=" + dataSplit[1];
    urlFormatLink = '<a id="link_' + i + '" href="' + urlFormat + '" target="_blank">ID: ' + dataSplit[0] + '</a>';

    return urlFormatLink;
}

function setLinkToWFIDs() {    
    $("#" + WFID_Data).val(array.join("|"));
}

function WFID_deleteWFID(id) {
	var deletedWFID = $("#link_" + id).text().replace("ID: ", "");
	for (var i = array.length - 1; i >= 0; i--) {
		if(array[i].split(':')[0] === deletedWFID) {
			array.splice(i, 1);
		}
	}	
    $("#row_" + id).remove();
    setLinkToWFIDs();
}

function paintLinkToWFIDs() {
    array = [];
    $('#WFID_Div').html('');
    var data = $("#" + WFID_Data).val().split("|");
    if (data != '') {
        for (i = 0; i < data.length; i++) {

            var statuslblWFID = $("#" + $("[id$='lblWorkflowStatus']").attr("id")).text();

            if (statuslblWFID == "Closed") {
                $('#WFID_Div').append('<div id="row_' + i + '" class="WFID_InternalLinkToWorkFlow">' + JoinUrlParameters(i, data[i]) + '&nbsp;&nbsp;&nbsp;</div>');
            }
            else {
                var wfDisabled = $("#" + $("[id$='WFID_Textbox']").attr("id")).attr('disabled');
                if (wfDisabled) {
                    $('#WFID_Div').append('<div id="row_' + i + '" class="WFID_InternalLinkToWorkFlow">' + JoinUrlParameters(i, data[i]) + '&nbsp;&nbsp;&nbsp;</div>');
                }
                else {
                    $('#WFID_Div').append('<div id="row_' + i + '" class="WFID_InternalLinkToWorkFlow">' + JoinUrlParameters(i, data[i]) + '<span class="xCloseEnabledLinkToWorkFlow" onclick="WFID_deleteWFID(' + i + ')"  id="close_' + i + '" >x</span><div>');
                }
                $("#" + $("[id$='WFID_buttonAdd']").attr("id")).attr('class', 'btn_blue_litle_LinkToWorkFlow');
            }
            array.push(data[i]);
        }
        setLinkToWFIDs();
    }
}

function closeLinkToWFWarning() {
    var messageModal = document.getElementsByClassName("modal")[0];
    messageModal.style.display = "none";
}

function closeForbiddenRemoveDocument() {
    var messageModal = document.getElementsByClassName("modal")[0];
    messageModal.style.display = "none";
}