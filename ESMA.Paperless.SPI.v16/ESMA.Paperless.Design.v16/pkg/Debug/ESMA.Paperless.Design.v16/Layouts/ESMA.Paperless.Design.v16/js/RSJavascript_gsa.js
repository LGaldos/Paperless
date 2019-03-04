//WORKFLOW FORM

//OPENING IN NEW TAB

function forceNewTab() {
    var documents = document.getElementsByTagName("A");

    for (var i = 0; i < documents.length; i++) {
        if (documents[i].hasAttribute("onclick")) {
            var onclickEvent = documents[i].getAttribute("onclick");

            if (onclickEvent.toString().toUpperCase().indexOf("DISPEX") >= 0) {
                documents[i].removeAttribute("onclick");
            }
        }
        if (documents[i].hasAttribute("target")) {
            documents[i].setAttribute("target", "_blank");
        } else {
            documents[i].addAttribute("target", "_blank");
        }

        if (documents[i].hasAttribute("onfocus")) {
            documents[i].removeAttribute("onfocus");
        }
    }
}

//END OF OPENING IN NEW TAB

//MANDATORY COMMENTS


function requireComments(DynamicRadioButtonListPanelClientID, btnAssignClientID, btnAssign2ClientID, lblCommentRequiredClientID, mandatoryCommentMessage, isRadio) {
    var radioButtonPanel = document.getElementById(DynamicRadioButtonListPanelClientID);

    if (radioButtonPanel != null) {
        var newComment = document.getElementById("NewCommentsArea");
        var newTextArea = newComment.getElementsByTagName("textarea");
        var comment = newTextArea[0].innerHTML.trim();

        if (isRadio === '1') {
            document.getElementById('RejectionUserSelected').innerHTML = 'UserSelected';
            document.getElementById('RejectionUserSelected').style.display = 'none';
        }

        if (document.getElementById("RejectionUserSelected").innerHTML !== '' && comment !== '') {
            document.getElementById(btnAssignClientID).disabled = false; 
            document.getElementById(btnAssign2ClientID).disabled = false;
        } else {
            document.getElementById(btnAssignClientID).disabled = true;
            document.getElementById(btnAssign2ClientID).disabled = true;
        }

        if (comment !== '') {
            document.getElementById(lblCommentRequiredClientID).innerHTML = '';
        } else {
            document.getElementById(lblCommentRequiredClientID).innerHTML = mandatoryCommentMessage;
        }
   }
}


//END OF MANDATORY COMMENTS

//TABS
function toggleTabs(id1, id2, id3, id4, id5, viewID1, viewID2, viewID3, viewID4, viewID5) {
    setCookie("RSSelectedTab", id1, 1);
    document.getElementById(id1).className = "Clicked";
    document.getElementById(viewID1).style.display = "block";
    document.getElementById(id2).className = "Initial";
    document.getElementById(viewID2).style.display = "none";
    document.getElementById(id3).className = "Initial";
    document.getElementById(viewID3).style.display = "none";
    document.getElementById(id4).className = "Initial";
    document.getElementById(viewID4).style.display = "none";
    document.getElementById(id5).className = "Initial";
    document.getElementById(viewID5).style.display = "none";
}

//END OF TABS

//EXPANDCOLLAPSE

function toggleAreas(id1, id2, id3, class1, class2) {
    var area = document.getElementById(id1);
    var areaTitle = document.getElementById(id2);
    var image = document.getElementById(id3);
    var cookieName = id1 + "COOKIE";

    if (area.style.display.toUpperCase() == "BLOCK") {
        setCookie(cookieName, "NONE", 365);
        area.style.display = "none";
        areaTitle.className = class1;
        image.src = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSPlus.gif";
        image.alt = "Expand";
    }
    else {
        setCookie(cookieName, "BLOCK", 365);
        area.style.display = "block";
        areaTitle.className = class2;
        image.src = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSMinus.gif";
        image.alt = "Collapse";
    }
}

function toggleAreasOnLoad(id1, id2, id3, class1, class2) {
    var area = document.getElementById(id1);
    var areaTitle = document.getElementById(id2);
    var image = document.getElementById(id3);
    var cookieName = id1 + "COOKIE";
    var cookieValue = getCookie(cookieName);

    if (cookieValue == "BLOCK") {
        area.style.display = "block";
        areaTitle.className = class2;
        image.src = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSMinus.gif";
        image.alt = "Collapse";        
    }
    else {
        area.style.display = "none";
        areaTitle.className = class1;
        image.src = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSPlus.gif";
        image.alt = "Expand";
    }
}

//END OF EXPANDCOLLAPSE

//GROUP CONFIDENTIAL FIELDS
function showConfidentialFields(idColumn1, idColumn2, idColumn3, valueToSelect, controlID) {
    var column1 = document.getElementById(idColumn1);
    var column2 = document.getElementById(idColumn2);
    var column3 = document.getElementById(idColumn3);
    var ddlConfidential = document.getElementById(controlID);

    if (column1 != null && column2 != null && column3 != null && ddlConfidential != null) {
        var selectedValue = ddlConfidential.options[ddlConfidential.selectedIndex].value;
        var areEqual = selectedValue.toUpperCase() === valueToSelect.toUpperCase();

        if (areEqual) {
            column1.style.display = 'block';
            column2.style.display = 'block';
            column3.style.display = 'block';
        }
        else 
        {
            column1.style.display = 'none';
            column2.style.display = 'none';
            column3.style.display = 'none';
        }
    }
}


function requireConfidentialValue(idColumn3, btnSaveID, btnSaveID2, btnSignID, btnSignID2, btnRejectID, btnRejectID2, btnAssignID, btnAssignID2, btnDeleteID, btnDeleteID2, actorAreaID, valueToSelect, confidentialListID, peopleEditorID) {
    var btnSave = document.getElementById(btnSaveID);
    var btnSign = document.getElementById(btnSignID);
    var btnReject = document.getElementById(btnRejectID);
    var btnAssign = document.getElementById(btnAssignID);
    var btnDelete = document.getElementById(btnDeleteID);
    var btnSave2 = document.getElementById(btnSaveID2);
    var btnSign2 = document.getElementById(btnSignID2);
    var btnReject2 = document.getElementById(btnRejectID2);
    var btnAssign2 = document.getElementById(btnAssignID2);
    var btnDelete2 = document.getElementById(btnDeleteID2);
    var ddlConfidential = document.getElementById(confidentialListID);
    var peopleEditor = document.getElementById(peopleEditorID);
    var actorArea = document.getElementById(actorAreaID);

    var selectedValue = ddlConfidential.options[ddlConfidential.selectedIndex].value;
    var areEqual = selectedValue.toUpperCase() === valueToSelect.toUpperCase();
	debugger;
    if (areEqual) {
        var entity = document.getElementById('divEntityData');
        var found = false;

        if (entity !== null) {
            var entityParentID = entity.parentNode.parentNode.id;

            if (entityParentID.toUpperCase().indexOf('CONFIDENTIAL') > -1) {
                found = true;
            }
        }
	

        if (found) {
            if (btnSave != null)
                btnSave.disabled = false;
            if (btnSave2 != null)
                btnSave2.disabled = false;
            if (btnSign != null)
                btnSign.disabled = false;
            if (btnSign2 != null)
                btnSign2.disabled = false;
            if (btnReject != null)
                btnReject.disabled = false;
            if (btnReject2 != null)
                btnReject2.disabled = false;
            if (btnAssign != null)
                btnAssign.disabled = false;
            if (btnAssign2 != null)
                btnAssign2.disabled = false;

            if (document.getElementById('ConfidentialErrorMessage') !== null)
                document.getElementById('ConfidentialErrorMessage').innerHTML = '';
        }
        else if (peopleEditor !== null && peopleEditor.disabled) {
            if (btnSave != null)
                btnSave.disabled = false;
            if (btnSave2 != null)
                btnSave2.disabled = false;
            if (btnSign != null)
                btnSign.disabled = false;
            if (btnSign2 != null)
                btnSign2.disabled = false;
            if (btnReject != null)
                btnReject.disabled = false;
            if (btnReject2 != null)
                btnReject2.disabled = false;
            if (btnAssign != null)
                btnAssign.disabled = false;
            if (btnAssign2 != null)
                btnAssign2.disabled = false;

            if (document.getElementById('ConfidentialErrorMessage') !== null)
                document.getElementById('ConfidentialErrorMessage').innerHTML = '';
        }
        else {
		
	    if(peopleEditor === null || peopleEditor.innerText.trim().substring(0,5) === "&#160"){
             if (btnSave != null)
                 btnSave.disabled = true;
             if (btnSave2 != null)
                 btnSave2.disabled = true;
             if (btnSign != null)
                 btnSign.disabled = true;
             if (btnSign2 != null)
                 btnSign2.disabled = true;
             if (btnReject != null)
                 btnReject.disabled = true;
             if (btnReject2 != null)
                 btnReject2.disabled = true;
             if (btnAssign != null)
                btnAssign.disabled = true;
             if (btnAssign2 != null)
                 btnAssign2.disabled = true;
            }
            else
            {
                if (btnSave != null)
                  btnSave.disabled = false;
                if (btnSave2 != null)
                  btnSave2.disabled = false;
                if (btnSign != null)
                  btnSign.disabled = false;
                if (btnSign2 != null)
                  btnSign2.disabled = false;
                if (btnReject != null)
                  btnReject.disabled = false;
                if (btnReject2 != null)
                  btnReject2.disabled = false;
                if (btnAssign != null)
                  btnAssign.disabled = false;
                if (btnAssign2 != null)
                  btnAssign2.disabled = false;
            }
	

            if (document.getElementById('ConfidentialErrorMessage') === null) {
                var node = document.createElement('div');
                node.id = 'ConfidentialErrorMessage';
                node.setAttribute('class', 'confidential_error_message');

                var textnode = document.createTextNode("");
                node.appendChild(textnode);
                document.getElementById(idColumn3).appendChild(node);
            }
            else if (document.getElementById('ConfidentialErrorMessage') !== null && document.getElementById('ConfidentialErrorMessage').innerHTML === '')
                document.getElementById('ConfidentialErrorMessage').innerHTML = "Confidential people is required.";
        }
    }
    else {
        if (btnSave != null)
            btnSave.disabled = false;
        if (btnSave2 != null)
            btnSave2.disabled = false;
        if (btnSign != null)
            btnSign.disabled = false;
        if (btnSign2 != null)
            btnSign2.disabled = false;
        if (btnReject != null)
            btnReject.disabled = false;
        if (btnReject2 != null)
            btnReject2.disabled = false;
        if (btnAssign != null)
            btnAssign.disabled = false;
        if (btnAssign2 != null)
            btnAssign2.disabled = false;

        if (document.getElementById('ConfidentialErrorMessage') !== null)
            document.getElementById('ConfidentialErrorMessage').innerHTML = '';
    }

}

//END OF GROUP CONFIDENTIAL FIELDS

//REASSIGNING

function showWaitingDialog(idMessage, idForm) {

    var messageWindow = document.getElementById(idMessage);
    messageWindow.style.display = "block";

    //document.getElementById(idForm).setAttribute("disabled", true);
    var controls = document.getElementsByTagName("input");
    for (var i = 0; i < controls.length; i++) {
        var controlType = controls[i].type.toString();
        controls[i].disable = true;
        controls[i].setAttribute("onclick", "this.disabled=\"disabled\"");
        controls[i].setAttribute("onchange", "this.disabled=\"disabled\"");
    }

    setTimeout(CloseWaiting, 4000)
}
function CloseWaiting() {
    var messageWindow = document.getElementById("ReassigningActor");
    messageWindow.style.display = "none";

    var controls = document.getElementsByTagName("input");
    for (var i = 0; i < controls.length; i++) {
        var controlType = controls[i].type.toString();
        controls[i].disable = true;
        controls[i].setAttribute("onclick", "this.disabled=\"enabled\"");
        controls[i].setAttribute("onchange", "this.disabled=\"enabled\"");
    }
}

//END OF REASSIGNING

//COOKIES

function getCookie(c_name) {
    var c_value = document.cookie;
    var c_start = c_value.indexOf(" " + c_name + "=");
    if (c_start == -1) {
        c_start = c_value.indexOf(c_name + "=");
    }
    if (c_start == -1) {
        c_value = null;
    }
    else {
        c_start = c_value.indexOf("=", c_start) + 1;
        var c_end = c_value.indexOf(";", c_start);
        if (c_end == -1) {
            c_end = c_value.length;
        }
        c_value = unescape(c_value.substring(c_start, c_end));
    }
    return c_value;
}

function setCookie(c_name, value, exdays) {
    var exdate = new Date();
    exdate.setDate(exdate.getDate() + exdays);
    var c_value = escape(value) + ((exdays == null) ? "" : "; expires=" + exdate.toUTCString());
    document.cookie = c_name + "=" + c_value;
}
//END OF COOKIES

//END OF WORKFLOW FORM

//REPORTS

function CleanHTMLWithin(id) {
    var item = document.getElementById(id);
    if (item != null) {
        item.innerHTML = "";
    }
}

function setFormSubmitToFalse() {
    setTimeout(function () { _spFormOnSubmitCalled = false; }, 3000);
    return true;
}

//END OF REPORTS