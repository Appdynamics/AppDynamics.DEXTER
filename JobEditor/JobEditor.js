// JavaScript source code
function helloWorld()
{
    window.alert("hi!");
    
    doDownload(document.getElementById("textAreaJobFile").innerHTML);
}

function dataUrl(data) 
{
    return "data:x-application/xml;charset=utf-8," + escape(data);
}


function doDownload(str)
{
    var downloadLink = document.createElement("a");
    downloadLink.href = dataUrl(str);
    downloadLink.download = "test.json";

    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}