//WORKFLOW FORM

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

//MODAL WINDOW
function showSPDialog(pageUrl) {
    var options = { url: pageUrl, width: 400, height: 300 };
    SP.SOD.execute('sp.ui.dialog.js', 'SP.UI.ModalDialog.showModalDialog', options);
}

//EXPANDCOLLAPSE

function showStepAreas(id1, id2, id3, class1, class2) {

    var area = document.getElementById(id1);
    var areaTitle = document.getElementById(id2);
    var image = document.getElementById(id3);
    var cookieName = id1 + "COOKIE";

    setCookie(cookieName, "BLOCK", 365);
    area.style.display = "block";
    areaTitle.className = class2;
    image.src = "/_layouts/15/ESMA.Paperless.Design.v16/images/RSMinus.gif";
    image.alt = "Collapse";
}

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

//REASSIGNING

function showWaitingDialog(idMessage, idForm, currentStep) {

    var messageWindow = document.getElementById(idMessage);
    var ddlControls = document.getElementsByTagName("DynamicUserListsPanel");
    //var showReassignMessage = false;

    //j = 0;

    //for (var i = 0; i < ddlControls.length; i++) {
        
    //    var controlClientId = ddlControls[i].id.toString();
    //    var control = document.getElementById(controlClientId);
        

    //    if (control != null && control.type == "select-one")
    //    {
    //        j = j + 1;

    //        if ((j = currentStep) && (control.options[control.selectedIndex].value != ""))
    //        {
    //            showReassignMessage = true;
    //            break;
    //        }

    //    }

    //}


    //if (showReassignMessage == true) {

        messageWindow.style.display = "block";

        document.getElementById(idForm).setAttribute("disabled", true);
        var controls = document.getElementsByTagName("input");
        for (var i = 0; i < controls.length; i++) {
            var controlType = controls[i].type.toString();
            controls[i].disable = true;
            controls[i].setAttribute("onclick", "this.disabled=\"disabled\"");
            controls[i].setAttribute("onchange", "this.disabled=\"disabled\"");
        }

    //}
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

//CALLOUT

function callout(sURLcontent, officeServer, sviews) {

    SP.SOD.executeFunc("callout.js", "Callout", function () {
    });

    var arrFiles = sURLcontent.split(";");
    var arrViews = sviews.split(";");

    for (a = 0; a < arrFiles.length; a++) {

        fView(arrFiles[a], arrViews[a], officeServer);
    }
}

function fView(arrFiles, strView, officeServer) {

    if (arrFiles.length > 0) {
        var tapsFiles = arrFiles.split(",");

        objView = document.getElementById(strView).getElementsByClassName("ms-vb2");

        var cont = 0;
        var a = 0;
        for (a = 0; a < objView.length; a++) {
            if (objView[a].getElementsByTagName("img").length > 0) {
                var files = tapsFiles[cont].toString().split("|");

                objView[a].innerHTML = '<a id="CallOutExample" onMouseOver="OpenItemFilePreviewCallOut(this, ' + "\'" + files[1] + "\'," + "\'" + officeServer + "/_layouts/15/WopiFrame2.aspx?sourcedoc=" + encodeURI(files[0]) + '\')" title="Preview Document" href="#"><img src="/_layouts/15/ESMA.Paperless.Design.v16/images/RSPreview.png" alt=""/></a>';
                cont++;
            }
        }
    }
}



function getCallOutFilePreviewBodyContent(urlWOPIFrameSrc, pxWidth, pxHeight) {

    var callOutContenBodySection = '<div class="js-callout-bodySection">';
    callOutContenBodySection += '<div class="js-filePreview-containingElement">';
    callOutContenBodySection += '<div class="js-frame-wrapper" style="line-height: 0">';
    callOutContenBodySection += '<iframe style="width: ' + pxWidth + 'px; height: ' + pxHeight + 'px;" src="' + urlWOPIFrameSrc + '&amp;action=interactivepreview&amp;wdSmallView=1" frameborder="0"></iframe>';
    callOutContenBodySection += '</div></div></div>';

    return callOutContenBodySection;
}

function OpenItemFilePreviewCallOut(sender, strTitle, urlWopiFileUrl) {
    //debugger;
    RemoveAllItemCallouts();

    var blnImage = CheckTypeFile(strTitle);
    if (blnImage == true) {
        var urlImage = urlWopiFileUrl.split('=');

        var modalImage = '<iframe id="iframe" onload="onLoadHandler();" style="width: 379px; height: 252px;" src="' + urlImage[1] + '" frameborder="0"></iframe>';

        var openNewWindow = true; //set this to false to open in current window

        var c = CalloutManager.getFromLaunchPointIfExists(sender);
        if (c == null) {
            c = CalloutManager.createNewIfNecessary({
                ID: 'CalloutId_' + sender.id,
                launchPoint: sender,
                beakOrientation: 'leftRight',
                title: strTitle,
                content: modalImage,
                contentWidth: 420
            });

        }
        c.open();
    }
    else {
        var openNewWindow = true; //set this to false to open in current window
        var callOutContenBodySection = getCallOutFilePreviewBodyContent(urlWopiFileUrl, 379, 252);

        var c = CalloutManager.getFromLaunchPointIfExists(sender);
        if (c == null) {
            c = CalloutManager.createNewIfNecessary({
                ID: 'CalloutId_' + sender.id,
                launchPoint: sender,
                beakOrientation: 'leftRight',
                title: strTitle,
                content: callOutContenBodySection,
                contentWidth: 420
            });

        }
        c.open();
    }
}

function onLoadHandler() {


    var iframe = document.getElementById('iframe')
    var doc = iframe.contentWindow.document;
    image = doc.body.getElementsByTagName('img');

    image[0].style.width = 379 + "px";
}


function RemoveAllItemCallouts() {
    CalloutManager.forEach(function (callout) {
        // remove the current callout
        CalloutManager.remove(callout);
    });
}

function RemoveItemCallout(sender) {
    var callout = CalloutManager.getFromLaunchPointIfExists(sender);
    if (callout != null) {
        // remove
        CalloutManager.remove(callout);
    }
}

function CloseItemCallout(sender) {
    var callout = CalloutManager.getFromLaunchPointIfExists(sender);
    if (callout != null) {
        // close
        callout.close();
    }
}

function CheckTypeFile(strTitle) {
    var type = false;
    var ext = ['gif', 'jpg', 'jpeg', 'png', 'icon', 'bmp', 'ico', 'tif'];

    var v = strTitle.split('.');
    v = v[1].toLowerCase();
    for (var i = 0; i < ext.length; i++) {
        if (ext[i].toLowerCase() == v)
            type = true;
    }
    return type;
}

SP.SOD.executeOrDelayUntilScriptLoaded(function () {
	filePreviewManager.previewers.extensionToPreviewerMap.pdf = 
		[embeddedWACPreview, WACImagePreview];
	embeddedWACPreview.dimensions.pdf= { width: 379, height: 252}
}, "filepreview.js");

//END CALLOUT