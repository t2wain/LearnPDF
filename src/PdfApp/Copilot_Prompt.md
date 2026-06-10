# Exploring Ideas

I'm using the following .NET nuget libraries in my application:

- PdfPig
- PdfPig.Rendering.Skia
- Tabula
- Tesseract
- OpenCvSharp4
- OpenCvSharp4.runtime.win
- iText

Given a PDF engineering drawing, I plan to extract various information from such drawing.

# Extract equipment tags

Plan and write code to extract equpment tags.

Equipment tags are alphanumerical identifiers embedded next to graphical symbol. An equipment tag has a pattern that can be detected using a regular expression.

# Extract drawing information from the title block

Plan and write code to extract drawing information the title block.

Each drawing is decorated with a title block template surronding the edges of the drawing. Information about the drawing is embedded within the title block at the bottom edge. Information about the drawing include:

- Drawing number
- Description
- Drawing revision number
- Revision date
- Designer initial
- Checker initial
- Approver initial
- Dates for each initial

Each of these information are embedded in a labled box arranged inside the title block.

# Extract reference of continuation drawing number

# Extract revision history from the revision table

# Extract text blocks from drawing

I'm using the following .NET nuget libraries in my application:

- PdfPig
- PdfPig.Rendering.Skia
- Tabula
- Tesseract
- OpenCvSharp4
- OpenCvSharp4.runtime.win

Write code to extract block of text from a PDF drawing. Each block of text are isolated from other blocks by spaces. A block of text may have more than one line of text. Line in the block are aligned in many different ways, center, left, or right aligned. A line of text may have more than one words. Each word must be separated by a space. Block of text might be rotated. All words are in upper case. Word are aligned center in the line.

The code is to be in C# consist of static functions in the functional programming style.