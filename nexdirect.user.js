// ==UserScript==
// @name         NexDirect v2
// @namespace    http://nicholastay.github.io/
// @homepage     https://github.com/nicholastay/NexDirect
// @version      0.2.6
// @icon         https://raw.githubusercontent.com/nicholastay/NexDirect/master/Designs/logo.png
// @description  Adds download button to page to use NexDirect & replaces the heart button on the listings -- You must visit the Settings panel (the logo in the bottom right) and register the URI scheme before you are able to use this script.
// @author       Nicholas Tay (Nexerq / @n2468txd) <nexerq@gmail.com>
// @license      https://github.com/nicholastay/NexDirect/blob/master/LICENSE
// @match        http*://osu.ppy.sh/*
// @grant        none
// @updateURL    https://raw.githubusercontent.com/nicholastay/NexDirect/master/nexdirect.user.js
// ==/UserScript==

(function() {
    'use strict';
    var nexDirectImage = "data:image/jpeg;base64,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"; 
    var miniIcon = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQAAAAUCAMAAAC6V+0/AAAB/lBMVEVYVaFeW6VfW6VhXqZiXqdiX6dnZKpqZ6udm8ienMihn8qhoMujoMujocurqdCvrdKysNS1tNbLyuLOzOPOzeTQz+TQz+XX1uja2erc2+vd3Oze3ezf3u3o5/Lr6vTs7PXt7PT6+v39/f7///9qZ6tqZ6ugnspqZqtqZ6tnZKpqZ6tqZ6t3dLKLib5oZapqZ6tqZ6tqZ6t7ebVnZKpqZ6tqZ6tqZ6tua65hXaZqZ6tiX6diX6dnZKpqZ6tqZ6tqZ6tmYqlqZ6tqZ6tqZ6tkYKijoctqZ6ujoctjYKfe3exqZ6tqZ6tqZqtqZ6tqZ6thXqZqZ6ujocujoctqZ6tqZ6tqZ6tqZ6tiX6dqZ6tqZ6uenMlqZ6uenMlqZ6tqZ6tqZ6tlYahqZ6tqZ6thXqZqZ6thXqZqZ6tqZ6tqZ6tqZ6tmZKpqZ6tqZqtqZ6tqZ6tqZ6tqZ6ujocujoctqZ6tqZ6tqZ6tqZ6toZatqZ6tqZqtqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tiX6dqZ6tiX6dqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tiX6dqZ6tiX6dqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tqZ6tmY6lmY6lqZ6tqZ6tqZ6tjYKdjYKdqZ6tiX6dqZ6vz5SM6AAAAqHRSTlMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAgIDAwQEBQUGBwcICQsMDA8REhQUFxgYGRobHBwdHh8iIyQlJSkqKy8zNDg7PEBBRkdISUtSVFRWWl5fYWJoaWtub3R5enx/f4GEjY6PkZSVlpeXrKyusLGys7S3t7i6u73AwcLDxMbM0tPU1NbW2tzd3+bq6+zu8PLz9vf3+Pn8/f4dXUYzAAABLklEQVQY02NQxgIYgFiOmxcIggrdQRSnCERQkomFlZW1ecUkayDFwAcRlGZT19ZiD120stuSXYWZHyqo6ZMSbcket3xlmxM7TFDGOGflrAht9oyVK6vhKqVsKmfPLbVk169buTIfJijk0TO5a4ofu1nxypUrUwUggoL+02sz5qcZ2jf1da9cFqMEFlQLn1sQ0Nvi7NVf79a+cqEnWFA3YUm6bd7cyOAZJbYdKxeABRWNshbHGgTPLEmak926cmkUWLu8efmcEBW7qmllcyauXJkMsUjCoWGqL7th4vye5StX5kKdJObS1+nIruLdOX/lygqYj0QtMuNN2NlVS1aurNGCCcrq6OlrsLOHzVvZZcoO97s4IyjoGldMsEIKOgUuHmDoBha5ggKZQxgiiAEAP3ZkllNXL9cAAAAASUVORK5CYII=";
    
    function welcome() {
        console.log("%cWelcome to NexDirect.", "font-size: 35px; line-height: 38px; font-weight: bold; color: white; text-shadow: 0 0 5px green;");
    }

    function log(t) {
        console.log("%c[NexDirect] %c" + t, "font-weight: bold;", "");
    }
    
    function injectListingPage() {
        // Replace the <3 icons with nexdirect
        log("Injecting download links into beatmap listing page...");
        var $heartIcons = $("div.beatmapListing .beatmap .bmlist-options i.icon-heart");
        if (!$heartIcons || ($heartIcons.length < 1))
            return log("Could not find any heart buttons to replace."); // rip
        
        log("Found " + $heartIcons.length + " heart icons. Replacing...");
        $heartIcons.each(function() {
            var $this = $(this);
            var $closestBeatmap = $this.closest(".beatmap");
            if (!$closestBeatmap || $closestBeatmap.length < 1)
                return log("Could not find closest beatmap for element");

            var beatmapSetId = $closestBeatmap.attr("id");
            if (!beatmapSetId)
                return log("Could not find beatmap set ID for element");
            
            $this.parent().attr("href", "nexdirect://" + beatmapSetId);
            $this.replaceWith("<div class=\"icon-nexdirect\"></div>");
        });
        log("Injected download buttons.");
        
        $('head').append("<style type=\"text/css\"> .icon-nexdirect { cursor: default; background-image: url(" + miniIcon + "); opacity: 0.5; width: 20px; height: 20px; display: block !important; margin-left: 2px; } .icon-nexdirect:hover { opacity: 1 } </style>");
        log("Injected additional CSS.");
    }
    
    function injectDownloadPage() {
        log("Injecting download button into beatmap download page...");
        // Get the beatmap *SET* id
        var $imgSrc = $("img.bmt").attr("src"); // jack from image preview src elem
        if (!$imgSrc)
            return log("Could not find image preview src for ID."); // rip

        log("Found image preview src element.");
        
        var beatmapSetId = $imgSrc.match(/\/(\d+)l\.jpg/);
        if (!beatmapSetId || !beatmapSetId[1])
            return log("Could not identify the beatmap set ID."); // rip
        beatmapSetId = beatmapSetId[1]; // its match 1

        var $downloadButton = $(".beatmapDownloadButton");
        if (!$downloadButton || $downloadButton.length < 1) {
            log("Could not find the beatmap download button. Probably not logged in, going to try use description box.");

            $downloadButton = $(".posttext");
            if (!$downloadButton || $downloadButton.length < 1) // it really shouldnt get here
                return log("Still cannot find element to inject.")
        }

        if ($downloadButton.length > 1) {
            // rotate 180 for lined up, usually dont because it stands out - https://stackoverflow.com/questions/14233341/how-can-i-rotate-an-html-div-90-degrees
            log("Found a no video/osu!supporter button, injecting after that one and rotating for conformity.");
            $downloadButton = $($downloadButton[0]);
            $('head').append("<style type=\"text/css\"> .nexdirectDownload { -webkit-transform: rotate(180deg); -moz-transform: rotate(180deg); -o-transform: rotate(180deg); -ms-transform: rotate(180deg); transform: rotate(180deg); } </style>");
        }
        $downloadButton.before("<div class=\"nexdirectDownload beatmapDownloadButton\"><a href=\"nexdirect://" + beatmapSetId + "\"><img src=\"" + nexDirectImage + "\" alt=\"Download with NexDirect\"/></a></div>");
        log("Injected download button.");
    }

    function injectNewListingPage(firstLoad) {
        log("[new] Finding new download button(s) to load into beatmap listing page...");
        var $dlIconBoxes = $("div.beatmapset-panel__icons-box:not([nexdirect-loaded])");
        if (!$dlIconBoxes || ($dlIconBoxes.length < 1))
            return log("Could not find any new download icons to add."); // rip
        
        log("Found " + $dlIconBoxes.length + " new download icons. Injecting NexDirect icons...");
        $dlIconBoxes.each(function() {
            var $this = $(this);
            var $idElem = $this.parents("div.beatmapset-panel").children("a.js-audio--play");
            if (!$idElem || ($idElem.length < 1))
                return log("Could not find ID elem."); // rip

            var beatmapSetId = $idElem.attr("data-audio-url").match(/\/(\d+)\.mp3$/);
            if (!beatmapSetId || !beatmapSetId[1])
                return log("Could not identify the beatmap set ID."); // rip
            beatmapSetId = beatmapSetId[1]; // its match 1

            $this.attr("nexdirect-loaded", "true"); // give it an attr to mark beatmap set as loaded
            $this.prepend("<a href=\"nexdirect://" + beatmapSetId + "\" class=\"beatmapset-panel__icon\"><img src=\"" + miniIcon + "\" alt=\"Download with NexDirect\" style=\"filter: brightness(0.35); -webkit-filter: brightness(0.35); opacity: 0.9; margin-bottom: 5px;\"/></a>");
        });
        log("Injected download icons to beatmap listing page.");
    }
    
    function injectNewDownloadPage() {
        log("[new] Injecting download button into beatmap download page...");
        // Get set id
        var $hrefElem = $("a[href*='/d/']");
        if (!$hrefElem)
            return log("Could not find link element."); // rip
        log("Found link element.");

        var beatmapSetId = $hrefElem[0].href.match(/\/d\/(\d+)n?/);
        if (!beatmapSetId || !beatmapSetId[1])
            return log("Could not retrieve beatmap set ID."); // rip
        beatmapSetId = beatmapSetId[1];
        log("Found beatmap set ID.");

        var $osuDirectButton = $(".beatmapset-header__buttons a").last();
        if (!$osuDirectButton || $osuDirectButton.text() !== "osu!direct")
            return log("Could not find osu!direct button to inject to."); // rip
        log("Found osu!direct button, modifying and injecting.");

        var $nexButton = $osuDirectButton.clone();
        $nexButton.attr("href", "nexdirect://" + beatmapSetId);
        $nexButton.attr("style", "filter: hue-rotate(284deg); -webkit-filter: hue-rotate(284deg);"); // green!
        $nexButton.find(".btn-osu-big__text-top").text("NexDirect");
        $nexButton.find("span.fa-download").replaceWith("<img src=\"" + miniIcon + "\" alt=\"NexDirect icon\" style=\"filter: brightness(0) invert(1); -webkit-filter: brightness(0) invert(1);\"/>");
        
        if ($osuDirectButton.attr("href").indexOf("osu://") >= 0) { // already supporter, dont delete the o!d elem but just inject after
            log("Detected osu!supporter, not going to delete the old element...");
            $osuDirectButton.after($nexButton);
        } else {
            log("Replacing o!d element...");
            $osuDirectButton.replaceWith($nexButton);
        }
        log("Injected download button.");
    }

    
    function detectInjection(firstLoad) {
        var newSite = typeof osu !== "undefined";
        var path = window.location.pathname;

        if (newSite) {
            if (/\/beatmapsets\/(\d+)/.test(path)) {
                if (!firstLoad)
                    log("----- PAGE CHANGE - beatmap page -----");
                injectNewDownloadPage();
            }
            else if (path.indexOf("/beatmapsets") === 0) {
                if (!firstLoad)
                    log("----- PAGE CHANGE - beatmap sets -----");
                injectNewListingPage(true);
            }

            if (firstLoad) { // new site uses turbolinks, so use this to check page changes
                log("[new] Attached turbolink cross-page handlers.");
                var $document = $(document);
                $document.on("turbolinks:load", function() { setTimeout(function() { detectInjection(); }, 650); }); // delay for page load change
                $document.on("beatmap:search:done", function() { log("--- BEATMAP SEARCHED ---"); injectNewListingPage(); }); // beatmap search page load complete bind
                $document.on("beatmap:load_more", function() {
                    if (NexDirect.loading) // need a loading var as for some reason the event gets fired 3 times each scroll as well
                        return;

                    NexDirect.loading = true;
                    log("--- VALID MORE BEATMAP LOADS REQUESTED ---");
                    setTimeout(function() {
                        injectNewListingPage();
                        setTimeout(function() { NexDirect.loading = false; }, 250); // allow loading again
                    }, 650); // delay to wait for load more to load and then inject again
                });
            }
        } else {
            if (path.indexOf("/p/beatmaplist") === 0)
                injectListingPage();
            else
                injectDownloadPage();
        }
    }

    window.NexDirect = {};
    welcome();
    
    setTimeout(function() {
        detectInjection(true);
    }, 650);
})();
