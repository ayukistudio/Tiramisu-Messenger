function filledCell(cell) {
    return cell !== '' && cell != null;
}

function loadFileData(filename, gk_isXlsx, gk_xlsxFileLookup, gk_fileData) {
    if (gk_isXlsx && gk_xlsxFileLookup[filename]) {
        try {
            const workbook = XLSX.read(gk_fileData[filename], { type: 'base64' });
            const worksheet = workbook.Sheets[workbook.SheetNames[0]];
            const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1, blankrows: false, defval: '' });
            const filteredData = jsonData.filter(row => row.some(filledCell));
            const headerRowIndex = filteredData.findIndex((row, index) =>
                row.filter(filledCell).length >= filteredData[index + 1]?.filter(filledCell).length
            ) || 0;

            const csv = XLSX.utils.sheet_to_csv(XLSX.utils.aoa_to_sheet(filteredData.slice(headerRowIndex)), { header: 1 });
            return csv;
        } catch (e) {
            console.error(e);
            return '';
        }
    }
    return gk_fileData[filename] || '';
}