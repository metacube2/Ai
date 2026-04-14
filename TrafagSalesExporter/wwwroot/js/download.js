window.trafagDownload = {
    saveTextFile: function (filename, content, contentType) {
        const blob = new Blob([content], { type: contentType || "application/json;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }
};
