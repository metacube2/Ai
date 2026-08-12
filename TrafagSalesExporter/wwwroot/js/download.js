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
    },
    saveBytes: function (filename, base64, contentType) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        const blob = new Blob([bytes], { type: contentType || "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    },
    printElement: function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) {
            window.print();
            return;
        }

        const title = element.querySelector(".hr-print-header h1")?.textContent || document.title;
        const printWindow = window.open("", "_blank", "noopener,noreferrer,width=1200,height=900");
        if (!printWindow) {
            window.print();
            return;
        }

        const styles = Array.from(document.querySelectorAll('link[rel="stylesheet"], style'))
            .map(node => node.outerHTML)
            .join("\n");

        printWindow.document.open();
        printWindow.document.write(`<!doctype html>
<html lang="de">
<head>
  <meta charset="utf-8">
  <title>${title}</title>
  ${styles}
  <style>
    @page { size: A4 landscape; margin: 10mm; }
    body { background: #fff !important; color: #111 !important; }
    .hr-print-toolbar, .mud-table-pagination, .mud-tabs-toolbar { display: none !important; }
    .mud-paper { box-shadow: none !important; border: 1px solid #ddd !important; break-inside: avoid; page-break-inside: avoid; }
    .mud-table-container { overflow: visible !important; }
    .mud-table-root { width: 100% !important; }
    .hr-print-section { display: block !important; }
    .hr-print-header { display: block !important; margin-bottom: 14px; }
    .hr-print-header h1 { margin: 0 0 4px 0; font-size: 22px; }
    .hr-print-header p { margin: 0 0 3px 0; color: #555; font-size: 11px; }
  </style>
</head>
<body>
  ${element.outerHTML}
  <script>
    window.onload = () => {
      setTimeout(() => {
        window.print();
        window.close();
      }, 250);
    };
  <\/script>
</body>
</html>`);
        printWindow.document.close();
    }
};
