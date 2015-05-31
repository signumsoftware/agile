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


export function createComment(createCommentUrl: string, buttonId: string, textAreaId: string, historyContainerId : string)
{
    buttonId.get().click(() => {
        var val = textAreaId.get().val();

        if (SF.isEmpty(val))
            return;

        SF.ajaxPost({ url: createCommentUrl, data: {text: val } }).then(html => {
            historyContainerId.get().empty().append(html);
        });
    });

}