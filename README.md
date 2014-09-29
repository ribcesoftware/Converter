RIBCE Books Converter
=====================

This software is used to convert the ebook files into a specific format that is used on the [RIBCE Book Lab](http://ribce.com "РИБСИ"). 
At the moment, this converter can process the files with old and new MS Word formats (*.doc and *.docx).
In the near future we want to add support for the following formats: 

- Adobe PDF (possible solution - [iTextSharp](http://sourceforge.net/projects/itextsharp/ "iTextSharp SourceForge"), [docs](http://www.afterlogic.com/mailbee-net/docs-itextsharp/ "iTextSharp Documentation"), [useful example](http://stackoverflow.com/questions/5945244/extract-image-from-pdf-using-itextsharp "iTextSharp StackOverflow"))
- EPUB (ePubReader [library](http://epubreader.codeplex.com/ "ePubReader on Codeplex"))
- FB2

If you want to help in the development of the RIBCE project - join.
Now this part of the project is **open source** software. 

How you can help: 
======================= 

1) Add support for the formats listed above. To do this, you will need to implement a class that inherits from the abstract class *Reader* (see the file Core.cs) As an example, note how the *MSWord_Reader* class is implemented. 

2) Improve the processing algorithm for MS Word files and, possibly, escape from the *Microsoft.Office.Interop* component dependency.

3) You can also help us with the server part of the project. To do this, you will need programming skills in PHP/MySQL.

For all inquiries please mail to contact@ribce.com
