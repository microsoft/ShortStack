
//------------------------------------------------------------------------------
// Break up a string so that it overflows cleanly
//------------------------------------------------------------------------------
export function formatToWidth(width: number, text: string)
{
    text += " ";
    const output = new Array<string>();
    let currentLine ="";
    let currentWord = "";
    let currentLineHasText = true;

    const flushLine = () =>
    {
        output.push(currentLine);
        currentLine = currentWord;
        currentLineHasText = currentWord != "";
        currentWord = "";
    }

    for(let i = 0; i < text.length; i++)
    {
        // skip space at the start of the line
        if(text[i] == ' ' && !currentLineHasText) continue;
        if(text[i].match(/\s/)) {
            if(currentWord !== "") {
                if((currentLine.length + currentWord.length) >= width) {
                    flushLine()
                }
                else {
                    currentLine += currentWord;
                    currentLineHasText = true;
                    currentWord = "";
                }
            }
            if(text[i] == '\n') flushLine();
            else currentLine += text[i];
        }
        else {
            currentWord += text[i];
        }
    }

    if(currentLine != "") flushLine();

    return output;
}
