window.editorHelpers = {
    insertTextAtCursor: function (elementId, textToInsert) {
        var el = document.getElementById(elementId);
        if (!el) return;

        var start = el.selectionStart;
        var end = el.selectionEnd;
        var text = el.value;
        var before = text.substring(0, start);
        var after = text.substring(end, text.length);

        el.value = before + textToInsert + after;
        el.selectionStart = el.selectionEnd = start + textToInsert.length;
        el.focus();

        // Trigger generic 'input' event so Blazor binds the new value
        var eventInput = new Event('input', { bubbles: true });
        el.dispatchEvent(eventInput);

        // Also trigger 'change' event for standard Blazor binding
        var eventChange = new Event('change', { bubbles: true });
        el.dispatchEvent(eventChange);
    }
};
