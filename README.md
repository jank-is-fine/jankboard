![splash](https://gist.github.com/user-attachments/assets/2277e581-aa79-4dac-a1dc-33dbd31d8b7c) 

# How to run

If you want to try out Jankboard, you can go to the releases page and download the executable for your OS.  
**Note:** This has not been tested on macOS at all. Windows has been tested only a little, and Linux is the only platform that’s actually been tested thoroughly.

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

That’s it, you are ready to start development.  

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
- It is possible to build for ARM but that depends on the deps, i did not do research on that part.

# Features

![ImageResizing](https://gist.github.com/user-attachments/assets/e7f5af19-7a27-452b-8b02-be2bd0c0b326) 

![GifAndImageSupport](https://gist.github.com/user-attachments/assets/3705f47b-2808-438f-b347-567b09f9dc1c)

![nesting](https://gist.github.com/user-attachments/assets/6a9f7191-4a0b-4eb6-a1a9-04bf2da19663)


# Final words

## Learning

I learned a good deal about OpenGL and GLSL, including (but not limited to) these highlights:

- Text rendering is hard.
- Rendering text is so hard.
- Oh my god, I want to rip my skin off,why is text rendering so hard?

## Limitations

- **Text rendering** could be way better. While I use MTSDF text rendering with vertices calculation each frame on the CPU and upload data each frame, a better approach would be using texture atlases with caching and dirty region updates.
- **The scene “system”** could be improved. Right now, I create a new list every frame, but this is overshadowed by the text rendering cost.
- **Code quality:** I added summaries, but there aren’t enough comments. 
  That said, I can’t really judge it since I know where everything is.

## Future of this project

I might revisit this project later, but it’ll be a while. In the meantime, if you want to improve or contribute, you’re probably better off forking the repo and doing it your way™.

## Font atlas creation
I used [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) , the command i use
`msdf-atlas-gen -font source -charset charset.txt -type mtsdf -size <size> -pxrange <pxrange> -imageout output.png -json output.json -yorigin top -pots`
where you can tweak pxrange and size.

![hampter](https://gist.github.com/user-attachments/assets/c48e04e7-51f3-4ba4-8c65-52d8797bb1eb)
Have fun.


# License

The code and project itself are **CC0**, but it uses other packages/libs with different licenses:

- [Silk.NET](https://github.com/dotnet/Silk.NET) (backbone of this project) : MIT 
- [ImageMagick](https://github.com/ImageMagick/ImageMagick) (loads texture data) : ImageMagick License
- [NativeFileDialogSharp](https://github.com/milleniumbug/NativeFileDialogSharp) (Windows file picker) : Zlib
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) (serialization) : MIT
- [SoundFlow](https://github.com/LSXPrime/SoundFlow) (audio) : MIT

These dependencies are pulled via NuGet.


Special thanks to:
[LearnOpenGL](https://learnopengl.com/) 
[msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen)


**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.**

For more information, go to the **License** tab in the repo.