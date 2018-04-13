// AUTHOR: Song Ho Ahn (song.ahn@gmail.com)
// CREATED: 2011-02-15
// UPDATED: 2018-01-19
//
// Copyright 2011. Song Ho Ahn

///////////////////////////////////////////////////////////////////////////////
var Logger = (function () {
    "use strict";

    ///////////////////////////////////////////////////////////////////////////
    // private members
    ///////////////////////////////////////////////////////////////////////////
    var version = "1.17";
    var containerDiv = null;
    var tabDiv = null;
    var logDiv = null;
    var logClear = null;
    var visible = true;     // flag for visibility
    var opened = false;     // flag for toggle on/off
    var enabled = true;     // does not accept log messages any more if it is false
    var logHeight = 215;    // 204 + 2*padding + border-top
    var tabHeight = 20;
    // for animation
    var animTime = 0;
    var animDuration = 200; // ms
    var animFrameTime = 16;  // ms

    ///////////////////////////////////////////////////////////////////////////
    // get time and date as string with a trailing space
    var getTime = function () {
        var now = new Date();
        var hour = "0" + now.getHours();
        hour = hour.substring(hour.length - 2);
        var minute = "0" + now.getMinutes();
        minute = minute.substring(minute.length - 2);
        var second = "0" + now.getSeconds();
        second = second.substring(second.length - 2);
        return hour + ":" + minute + ":" + second;
    };
    var getDate = function () {
        var now = new Date();
        var year = "" + now.getFullYear();
        var month = "0" + (now.getMonth() + 1);
        month = month.substring(month.length - 2);
        var date = "0" + now.getDate();
        date = date.substring(date.length - 2);
        return year + "-" + month + "-" + date;
    };
    ///////////////////////////////////////////////////////////////////////////
    // return available requestAnimationFrame(), otherwise, fallback to setTimeOut
    var getRequestAnimationFrameFunction = function () {
        var requestAnimationFrame = window.requestAnimationFrame ||
            window.mozRequestAnimationFrame ||
            window.msRequestAnimationFrame ||
            window.oRequestAnimationFrame ||
            window.webkitRequestAnimationFrame;
        if (requestAnimationFrame)
            return function (callback) { return requestAnimationFrame(callback); };
        else
            return function (callback) { return setTimeout(callback, 16); };
    };



    ///////////////////////////////////////////////////////////////////////////
    // public members
    ///////////////////////////////////////////////////////////////////////////
    var self =
        {
            ///////////////////////////////////////////////////////////////////////
            // create a div for log and attach it to document
            init: function () {
                // avoid redundant call
                if (containerDiv)
                    return true;

                // check if DOM is ready
                if (!document || !document.createElement || !document.body || !document.body.appendChild)
                    return false;

                // constants
                var CONTAINER_DIV = "loggerContainer";
                var TAB_DIV = "loggerTab";
                var LOG_DIV = "logger";
                var LOG_CLEAR = "logClear";
                var Z_INDEX = 9999;

                // create logger DOM element
                containerDiv = document.getElementById(CONTAINER_DIV);
                if (!containerDiv) {
                    // container
                    containerDiv = document.createElement("div");
                    containerDiv.id = CONTAINER_DIV;
                    containerDiv.setAttribute("style", "width:100%; " +
                        "margin:0; " +
                        "padding:0; " +
                        "text-align:left; " +
                        "box-sizing:border-box; " +
                        "position:fixed; " +
                        "left:0; " +
                        "z-index:" + Z_INDEX + "; " +
                        "bottom:" + (-logHeight) + "px; ");  /* hide it initially */

                    // tab
                    tabDiv = document.createElement("div");
                    tabDiv.id = TAB_DIV;
                    tabDiv.appendChild(document.createTextNode("Log"));
                    tabDiv.setAttribute("style", "width:40px; " +
                        "box-sizing:border-box; " +
                        "overflow:hidden; " +
                        "font:bold 10px verdana,helvetica,sans-serif; " +
                        "line-height:" + (tabHeight - 1) + "px; " +  /* subtract top-border */
                        "color:#fff; " +
                        "position:absolute; " +
                        "left:20px; " +
                        "top:" + -tabHeight + "px; " +
                        "margin:0; padding:0; " +
                        "text-align:center; " +
                        "border-bottom:none; " +
                        "background:rgba(0, 0, 0, 0.8); " +
                        "border-top-right-radius:3px; " +
                        "border-top-left-radius:3px; ");


                    // log clear
                    logClear = document.createElement("div");
                    logClear.id = LOG_CLEAR;
                    logClear.appendChild(document.createTextNode("Clear"));
                    logClear.setAttribute("style", "width:40px; " +
                        "box-sizing:border-box; " +
                        "overflow:hidden; " +
                        "font:bold 10px verdana,helvetica,sans-serif; " +
                        "line-height:" + (tabHeight - 1) + "px; " +  /* subtract top-border */
                        "color:#fff; " +
                        "position:absolute; " +
                        "left:63px; " +
                        "top:" + -tabHeight + "px; " +
                        "margin:0; padding:2; " +
                        "text-align:center; " +
                        "border-bottom:none; " +
                        "background:rgba(0, 0, 0, 0.8); " +
                        "border-top-right-radius:3px; " +
                        "border-top-left-radius:3px; ");

                    logClear.onmouseover = function () {
                        this.style.cursor = "pointer";
                    };

                    logClear.onclick = function () {
                        Logger.clear();
                    };

                    tabDiv.onmouseover = function () {
                        this.style.cursor = "pointer";
                    };
                    tabDiv.onmouseout = function () {
                        this.style.cursor = "auto";
                        this.style.textShadow = "none";
                    };
                    tabDiv.onclick = function () {
                        Logger.toggle();
                        this.style.textShadow = "none";
                    };

                    // log message
                    logDiv = document.createElement("div");
                    logDiv.id = LOG_DIV;
                    logDiv.setAttribute("style", "font:12px monospace; " +
                        "height: " + logHeight + "px; " +
                        "box-sizing:border-box; " +
                        "color:#fff; " +
                        "overflow-x:hidden; " +
                        "overflow-y:hidden; " +
                        "visibility:hidden; " +
                        "position:relative; " +
                        "bottom:0px; " +
                        "margin:0px; " +
                        "padding:5px; " +
                        "background:rgba(0, 0, 0, 0.8); ");

                    // style for log message
                    var span = document.createElement("span");  // for coloring text
                    span.style.color = "#afa";
                    span.style.fontWeight = "bold";

                    // the first message in log
                    var msg = "===== Log Started at " +
                        getDate() + ", " + getTime() + "";

                    span.appendChild(document.createTextNode(msg));
                    logDiv.appendChild(span);
                    logDiv.appendChild(document.createElement("br"));   // blank line
                    logDiv.appendChild(document.createElement("br"));   // blank line

                    // add divs to document
                    containerDiv.appendChild(tabDiv);
                    containerDiv.appendChild(logClear);
                    containerDiv.appendChild(logDiv);
                    document.body.appendChild(containerDiv);
                }

                return true;
            },
            ///////////////////////////////////////////////////////////////////////
            // print log message to logDiv
            print: function (msg, color) {
                // ignore message if it is disabled
                if (!enabled)
                    return;

                // check if this object is initialized
                if (!containerDiv) {
                    var ready = this.init();
                    if (!ready)
                        return;
                }

                var msgDefined = true;

                // convert non-string type to string
                if (typeof msg == "undefined")       // print "undefined" if param is not defined
                {
                    msg = "undefined";
                    msgDefined = false;
                }
                else if (typeof msg == "function")   // print "function" if param is function ptr
                {
                    msg = "function";
                    msgDefined = false;
                }
                else if (msg === null)               // print "null" if param has null value
                {
                    msg = "null";
                    msgDefined = false;
                }
                else {
                    if (msg instanceof Array)        // print array elements if param is array object
                    {
                        msg = this.arrayToString(msg);
                    }
                    else if (msg instanceof Object)  // print "object" if param is object type
                    {
                        msg = "object";
                        msgDefined = false;
                    }
                    else {
                        msg += ""; // for other types
                    }
                }

                var lines = msg.split(/\r\n|\r|\n/);
                for (var i = 0, c = lines.length; i < c; ++i) {
                    // format time and put the text node to inline element
                    var timeDiv = document.createElement("div");            // color for time
                    timeDiv.setAttribute("style", "color:#999;" +
                        "float:left;");

                    var timeNode = document.createTextNode(getTime() + "\u00a0");
                    timeDiv.appendChild(timeNode);

                    // create message span
                    var msgDiv = document.createElement("div");
                    msgDiv.setAttribute("style", "word-wrap:break-word;" +  // wrap msg
                        "margin-left:6.0em;");     // margin-left = 9 * ?

                    if (color) {
                        msgDiv.style.color = color;
                    }

                    if (!msgDefined)
                        msgDiv.style.color = "#afa";

                    // put message into a text node
                    var line = lines[i].replace(/ /g, "\u00a0");
                    var msgNode = document.createTextNode(line);
                    msgDiv.appendChild(msgNode);

                    // new line div with clearing css float property
                    var newLineDiv = document.createElement("div");
                    newLineDiv.setAttribute("style", "clear:both;");

                    logDiv.appendChild(timeDiv);            // add time
                    logDiv.appendChild(msgDiv);             // add message
                    logDiv.appendChild(newLineDiv);         // add message

                    logDiv.scrollTop = logDiv.scrollHeight; // scroll to last line
                }
            },
            ///////////////////////////////////////////////////////////////////////
            // slide log container up and down
            toggle: function () {
                if (opened)  // if opened, close the window
                    this.close();
                else        // if closed, open the window
                    this.open();
            },
            open: function () {
                if (!this.init()) return;
                if (!visible) return;
                if (opened) return;

                logDiv.style.visibility = "visible";
                animTime = Date.now();
                var requestAnimationFrame = getRequestAnimationFrameFunction();
                requestAnimationFrame(slideUp);
                function slideUp() {
                    var duration = Date.now() - animTime;
                    if (duration >= animDuration) {
                        containerDiv.style.bottom = 0;
                        opened = true;
                        return;
                    }
                    var y = Math.round(-logHeight * (1 - 0.5 * (1 - Math.cos(Math.PI * duration / animDuration))));
                    containerDiv.style.bottom = "" + y + "px";
                    requestAnimationFrame(slideUp);
                }
            },
            close: function () {
                if (!this.init()) return;
                if (!visible) return;
                if (!opened) return;

                animTime = Date.now();
                var requestAnimationFrame = getRequestAnimationFrameFunction();
                requestAnimationFrame(slideDown);
                function slideDown() {
                    var duration = Date.now() - animTime;
                    if (duration >= animDuration) {
                        containerDiv.style.bottom = "" + -logHeight + "px";
                        logDiv.style.visibility = "hidden";
                        opened = false;
                        return;
                    }
                    var y = Math.round(-logHeight * 0.5 * (1 - Math.cos(Math.PI * duration / animDuration)));
                    containerDiv.style.bottom = "" + y + "px";
                    requestAnimationFrame(slideDown);
                }
            },
            ///////////////////////////////////////////////////////////////////////
            // show/hide the logger window and tab
            show: function () {
                if (!this.init())
                    return;

                containerDiv.style.display = "block";
                visible = true;
            },
            hide: function () {
                if (!this.init())
                    return;

                containerDiv.style.display = "none";
                visible = false;
            },
            ///////////////////////////////////////////////////////////////////////
            // when Logger is enabled (default), log() method will write its message
            // to the console ("logDiv")
            enable: function () {
                if (!this.init())
                    return;

                enabled = true;
                tabDiv.style.color = "#fff";
                logDiv.style.color = "#fff";
            },
            ///////////////////////////////////////////////////////////////////////
            // when it is diabled, subsequent log() calls will be ignored and
            // the message won't be written on "logDiv".
            // "LOG" tab and log text are grayed out to indicate it is disabled.
            disable: function () {
                if (!this.init())
                    return;

                enabled = false;
                tabDiv.style.color = "#444";
                logDiv.style.color = "#444";
            },
            ///////////////////////////////////////////////////////////////////////
            // clear all messages from logDiv
            clear: function () {
                if (!this.init())
                    return;

                logDiv.innerHTML = "";
            },
            ///////////////////////////////////////////////////////////////////////
            // utility funtions
            arrayToString: function (array) {
                var str = "[";
                for (var i = 0, c = array.length; i < c; ++i) {
                    if (array[i] instanceof Array)
                        str += this.arrayToString(array[i]);
                    else
                        str += array[i];

                    if (i < c - 1)
                        str += ", ";
                }
                str += "]";
                return str;
            }
        };
    return self;
})();
