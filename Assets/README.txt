Place your application icon file here as `appicon.ico`.

You currently have `Icon.jpg` at the project root. To make the `Icon` attribute in the .axaml files work as set (Icon="Assets/appicon.ico"), convert or export that image to Windows icon format (`.ico`) and save it to:

Assets\appicon.ico

If you prefer a PNG you can also use `Assets/appicon.png` but then update the `Icon` attribute in the .axaml files accordingly.

To convert locally on Windows you can use PowerShell with ImageMagick (if installed):

magick "C:\Users\brawl\OneDrive\Рабочий стол\ShopApp\Icon.jpg" -define icon:auto-resize=64,48,32,16 Assets\appicon.ico

Or use any online JPG→ICO converter and place the resulting file at `Assets\appicon.ico`.