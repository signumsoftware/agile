/// <reference path="../../framework/signum.web/signum/scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function unarchive(options: Operations.OperationOptions, findOptions: Finder.FindOptions, url: string): Promise<void> {

    options.controllerUrl = url;

    return Finder.find(findOptions).then(list=> {

        if (!list)
            return;

        options.requestExtraJsonData = { list: list.key() };

        return Operations.executeDefault(options);
    });
}


export function attachTags(entityList: Lines.EntityListCheckbox) {

    entityList.element.on("change", "input[type=checkbox]", e=> {

        var label = $(e.currentTarget).closest("label");

        if ($(e.currentTarget).is(":checked")) {
            label.css("background-color", label.css("border-color"));
            label.css("color", "white");
        } else {
            label.css("background-color", "");
            label.css("color", "");
        }
    });
}


export function createComment(createCommentUrl: string, buttonId: string, textAreaId: string, historyContainerId: string) {
    buttonId.get().click(() => {
        var val = textAreaId.get().val();

        if (SF.isEmpty(val))
            return;

        SF.ajaxPost({ url: createCommentUrl, data: { text: val } }).then(html => {
            textAreaId.get().val(null);
            historyContainerId.get().empty().append(html);
        });
    });
}

export function uploadDocument(uploadUrl: string, historyContainerId: string) {
    if (window.File && window.FileList && window.FileReader && new XMLHttpRequest().upload) {
        var $fileDrop = $(".drop-target")
            .on("dragenter", (e) => {
            e.stopPropagation();
            e.preventDefault();
            dragIncrement(1);
        }).on("dragleave", (e) => {
            e.stopPropagation();
            e.preventDefault();
            dragIncrement(-1);
        }).on("dragover", (e) => {
            e.stopPropagation();
            e.preventDefault();
        });

        $fileDrop[0].addEventListener("drop", (e) => {
            e.stopPropagation();
            e.preventDefault();
            dragIncrement(-1);
            var files = e.dataTransfer.files;
            uploadingIncrement(files.length);
            for (var i = 0; i < files.length; i++) {
                upload(files[i], uploadUrl, historyContainerId);
            }

        }, false);

        $(".drop-target").on("paste", (e) => {
            var ce = <ClipboardEvent><any> e.originalEvent;

            for (var i = 0; i < ce.clipboardData.items.length; i++) {
                var item = ce.clipboardData.items[i];
                console.log("Item: " + item.type);
                if (item.type.indexOf("image/") != -1) {
                    var f = (<any>item).getAsFile()
                    f.name = "image." + item.type.after("image/");
                    upload(f, uploadUrl, historyContainerId);
                } else {
                    return false;
                }
            }
        });
    }
}

function upload(f: File, uploadUrl: string, historyContainerId: string) {
    var xhr = new XMLHttpRequest();
    xhr.open("POST", uploadUrl, true);
    xhr.setRequestHeader("X-FileName", f.name);

    var self = this;
    xhr.onload = (e) => {
        historyContainerId.get().empty().append(xhr.responseText);
        uploadingIncrement(-1);
    };

    xhr.onerror = (e) => {
        SF.Notify.error(xhr.statusText);
        uploadingIncrement(-1);
    };

    xhr.send(<any>f);
}


var uploading = 0;
function uploadingIncrement(inc: number) {
    uploading += inc;
    $(".drop-uploading").toggle(uploading > 0);
}


var drag = 0;
function dragIncrement(inc: number) {
    console.log("drag:" + drag + ", inc:" + inc);
    drag += inc;
    $(".drop-here").toggle(drag > 0);
}