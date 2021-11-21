# How to contribute
- Found a **bug**? [Open an issue](https://github.com/Fs00/Win10BloatRemover/issues/new) detailing the problem you encountered.
- Have an **idea** on how to improve the tool or want to **request a feature**? [Start a new discussion](https://github.com/Fs00/Win10BloatRemover/discussions/new) and let's talk about it!
- Want to contribute with **code**? If you plan to extend the program, [open a discussion](https://github.com/Fs00/Win10BloatRemover/discussions/new) before starting your work (so that we can evaluate whether there is enough interest in what you'd like to implement), otherwise feel free to send a pull request.  
Please follow the practices described below for a better review experience 😉

### Best practices for pull requests
- Make sure to tick "Allow edits from maintainers" when you open a pull request
- Try to keep your code as simple and self-documenting as possible. The following tips may be helpful:
  - a long but descriptive function/variable name is better than a concise but less clear one
  - keep functions short and focused (two levels of indentation at most is a good heuristic)
  - comments should only be used to explain *why* your code does something, not *what* the code is doing
- Avoid formatting changes to unrelated files
- Please follow these styling guidelines:
  - [Official .NET naming guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
  - Place brackets on the next line, except for anonymous functions and array/dictionary/class initializers
  - Do not use brackets for one-line code blocks (unless they are mandatory, e.g. try/catch blocks)