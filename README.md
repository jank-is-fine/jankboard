<img alt="jankboard-splash" src="https://github.com/user-attachments/assets/bb7f4798-ea29-4778-85a8-ca1d7154c754" />


# How to run

You need the dotnet desktop runtime to be installed to run this.
Go to [https://dotnet.microsoft.com/en-us/download/dotnet/9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) and follow the instructions to install it. 

If you want to try out Jankboard, you can go to the [releases](https://github.com/jank-is-fine/jankboard/releases) page and download the executable for your OS.  

**Note:**
Testing status:
- MacOS: Not tested at all. 
- Windows: Tested a bit on Windows 10 and 11.
- Linux: Tested on Wayland thoroughly. Your mileage on other distros might vary.

# Setup

## Prerequisites

 **.NET 9 SDK**  
   Follow the official guide for your OS:  
   [https://dotnet.microsoft.com/en-us/download/dotnet/9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
   

## Once prerequisites are met

Clone the repo
```
git clone https://github.com/jank-is-fine/jankboard.git
```

Restore/pull packages via Dotnet
```
dotnet restore
```

That’s it, you are ready to start development or build from source.  

## Building from source
To build a release version for your platform:
```
dotnet publish -c Release -r <OS>
```
**Available platforms:**
- `linux-x64`
- `win-x64`
- `osx-x64`

**Notes:**
- Tested with Windows and Linux (Wayland).
- A `.pdb` file will appear alongside the executable; this is only for debugging and can be ignored unless you're troubleshooting crashes.
- It is possible to build for ARM but that depends on the deps, I did not do research on that part.

# Features

## BBCODE -ish text formatting
Features some BBCODE like formatting:
### Color override:
[color=\<known color>] or [color=\<hexcode>] so you can either set it to [color=red] or [color=#8B0000] for example. Closing tag is [/color]

### Font overrides: 
- [b] Bold Text [/b]
- [i] Italic text [/i]
- [b][i] Bold-Italic text [/i][/b]
- [i][b] Order does not matter [/b][/i]

[b] and [i] require closing tags ([/b], [/i]). Once opened, they stay active until closed - so you can put them at the start of a paragraph and forget about closing per line.

---

### Per line Overrides:
- [align=\<center, right>] aligns the whole line
- [size=\<int>] sets the font size for the whole line

For these there are not closing tags as this is on a line basis overrides and will not apply for the next lines.

## Hotkeys
### In the Text editor/Inputfield
Copy: ctrl + c

Paste: ctrl + v

Jump word: ctrl + arrow keys

Select: shift + arrow keys

Select word: ctrl + shift + arrow keys 

Select all: ctrl + a

## Scene view:
Copy selected Objects: ctrl + c

This does copy all of the selected elements.
This works for connections as well, but you will need to have the connecting entries selected as well.

Paste selected objects: ctrl + v

This works on different saves, you can copy over objects from one save to the other.

I am open for suggestions, create an issue for them and I might add them when I have the time for it.



---
## Showcase

![ImageResizing](https://github.com/user-attachments/assets/a3752e88-6f92-496f-8181-fac65f746bfd)

![GifAndImageSupport](https://github.com/user-attachments/assets/72a27506-512a-43cb-9b89-4ac52b729480)

![nesting](https://github.com/user-attachments/assets/4c2311f5-83b2-4096-88b9-93d926318c49)


# Final words

## Learning

I learned a good deal about OpenGL and GLSL, including (but not limited to) these highlights:

- Text rendering is hard.
- Rendering text is so hard.
- Oh my god, I want to rip my skin off, why is text rendering so hard?

## Limitations

- ~~Text rendering could be way better.~~
- ~~The scene “system” could be improved. Right now, I create a new list every frame, but this is overshadowed by the text rendering cost.~~
- **Code quality:** I added summaries, but there aren’t enough comments. 
  That said, I can’t really judge it since I know where everything is.

## Future of this project

If you want to improve or contribute, you’re probably better off forking the repo and doing it your way™.
I do plan on working on this project maybe further but I might not see your PRs fast enough. This is a hobby project after all.

## Font atlas creation
I used [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) , the command I use
```bash
msdf-atlas-gen -font source -charset charset.txt -type mtsdf -size <size> -pxrange <pxrange> -imageout output.png -json output.json -yorigin top -pots
```
where you can tweak pxrange and size.

**NOTES:**
Make sure to set the pxrange to a high enough value so the text rendering stays sharp. 

The -pots flag is optional.

charset.txt holds the glyphs that get used to generate the atlas, you can grab the one I used [here](https://gist.github.com/jank-is-fine/960f9de3c09e1f58fd60ee45c541130c) .

Not all of the glyphs of this charset are in the fonts I used but I had decided on a charset on a whim to be honest.


# License

The code and project itself are **CC0**, but it uses other packages/libs with different licenses:

- [Silk.NET](https://github.com/dotnet/Silk.NET) (backbone of this project) : MIT 
- [ImageMagick](https://github.com/ImageMagick/ImageMagick) (loads texture data) : ImageMagick License
- [NativeFileDialogSharp](https://github.com/milleniumbug/NativeFileDialogSharp) (Windows file picker) : Zlib
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (serialization) : MIT
- [SoundFlow](https://github.com/LSXPrime/SoundFlow) (audio) : MIT

These dependencies are pulled via NuGet/dotnet (restore) 


Special thanks to:
[LearnOpenGL](https://learnopengl.com/) 
[msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen)


**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**

For more information, go to the **License** tab in the repo.
