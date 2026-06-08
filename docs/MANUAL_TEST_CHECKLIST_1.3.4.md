# Manual test checklist — JC Lib Visual Studio 1.3.4

1. Build the VSIX under Visual Studio 2026.
2. Open the JC Lib tool window.
3. Load `build_pack.json`.
4. Select `gcc shared library`.
5. Confirm the preview starts with `gcc ` and not `gcc shared library(`.
6. Enable `-L`, select `lib`, and enable `-lm`.
7. Confirm the preview resembles `gcc -Wall -Wextra -O2 -fPIC -shared src/mylib.c -o "libmylib.so" -Llib -lm`.
8. Select `cmake_minimum_required` and confirm the preview does not end with `;`.
9. Select `cmake configure source build` and confirm the preview is a shell command.
