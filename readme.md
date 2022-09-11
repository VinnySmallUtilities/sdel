Русский ниже

# English
The program erase the file (directory) with a single rewriting (data sanitization) of the data in it.
A simple file overwrite is used through the OS functions, do not expect anything special or complex.

The file is deleted as follows:
1) Each byte of the file is rewrited, but not beyond its end (for example, a file with a length of 24 bytes will be rewrite with 24 bytes)
    The rewriting pattern: 0x55AA (101010110101010b)
    With the "z" flag, the rewriting pattern is 0x00 (zero)
2) The file system block size is not known to the program. It works at too much a high level.
3) The file size is expanded to a multiple of 65536 bytes (0x10000) by the same pattern
4) The file is renamed. The characters of the file name are replaced with spaces with the same length.
    For example, the file "a.txt " will be renamed to "     ".
5) The file is reopened with the "File.truncate" flag. That is, when opening a file, the OS will trim this file to zero size (it will be harder to understand how much the file weighed)
6) The file is deleted by the usual means of the OS


# Русский
Программа удаляет файл (папку) с однократным перезатированием данных в нём.
Используется простая перезапись файла через функции ОС, не ждите ничего особенного.

Удаление файла происходит следующим образом:
1) Перезатирается каждый байт файла, но не далее его конца (например, файл, длиной 24 байта, перезатрётся 24-мя байтами)
    Шаблон перезатирания: 0x55AA (101010110101010b)
    С флагом z шаблон перезатирания 0x00 (ноль)
2) Размер блока файловой системы не известен программе. Она работает на слишком высоком уровне.
3) Размер файла дополняется до кратного 65536 байтам (0x10000) тем же шаблоном
4) Файл переименовывается. Символы имени файла перезатираются пробелами с той же длиной. Например, файл "a.txt" будет переименован в "     "
5) Файл переоткрывается с флагом File.truncate. То есть при открытии файла, ОС обрежет этот файл до нулевого размера (тяжелее будет понять, сколько весил файл)
6) Файл удаляется обычыми средствами ОС
