$(document).ready(function () {
    function GetIEVersion() {
        var sAgent = window.navigator.userAgent;
        var Idx = sAgent.indexOf("MSIE");

        // If IE, return version number.
        if (Idx > 0)
            return parseInt(sAgent.substring(Idx + 5, sAgent.indexOf(".", Idx)));

        // If IE 11 then look for Updated user agent string.
        else if (navigator.userAgent.match(/Trident\/7\./))
            return 11;
        else
            return 0; //It is not IE
    }

    if (GetIEVersion() > 0) {
        $('body').addClass("ie11");
    }
});


var wref = null;
function closeWindow() {
    if (wref !== null) {
        console.log('window.close()');
        wref.close();
        $("body").removeClass("popup-active");
    }
    wref = null;
}
window.onfocus = function () {
    if (wref !== null && typeof wref.window === 'object') {
        console.log('onfocus, setting focus on window.');
        window.setTimeout(function () {
            wref.focus();
        }, 100);
    } else {
        console.log('onfocus, but not setting focus on window..');
    }
};

function popupwindow(url, title, w, h) {
    $("body").addClass("popup-active");
    var y = window.outerHeight / 2 + window.screenY - (h / 2);
    var x = window.outerWidth / 2 + window.screenX - (w / 2);
    wref = window.open(url, title, 'toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=no, resizable=no, copyhistory=no, width=' + w + ', height=' + h + ', top=' + y + ', left=' + x);
    wref.focus();
}
