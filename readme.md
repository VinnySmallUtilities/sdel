Русский ниже

# English
For Linux require to install [.NET 7.0](https://dotnet.microsoft.com/download)

Building for you system
    cd you_directory_for_sdel
    git clone https://github.com/VinnySmallUtilities/sdel
    cd sdel
    dotnet publish --output ./build -c Release --self-contained false /p:PublishSingleFile=true


The program erase the file (directory) with a single rewriting (data sanitization) of the data in it.

A simple file overwrite is used through the OS functions, do not expect anything special or complex.

Flags are entered in a non-standard way. For example:

sdel vvz2pr /home/user/.wine

These are the flags vv, z2, pr


The same flags can be entered as

sdel vv_z2_pr /home/user/.wine

sdel "vv z2 pr" /home/user/.wine


Running without flags

sdel - /home/user/.wine


The file is deleted as follows:
1) Each byte of the file is rewrited, but not beyond its end (for example, a file with a length of 24 bytes will be rewrite with 24 bytes)

    The rewriting pattern: 0x55AA (101010110101010b)
    
    With the "z"  flag, the rewriting pattern is 0x00 (zero)
    
    With the "z2" flag, the rewriting patterns is 0x55AA and 0x0000
    
    With the "z3" flag, the rewriting patterns is (0xCCCC 0x6666 0x00)
    
    1100110011001100
    
    0110011001100110
    
    0000000000000000
    

2) The file system block size is not known to the program. It works at too much a high level.
3) The file size is expanded to a multiple of 65536 bytes (0x10000) by the same pattern
    If the wiping occurs several times, then the wiping of additional space occurs after all times of the wiping of the entire file has been completed.
4) The file is renamed. The characters of the file name are replaced with spaces with the same length.
    For example, the file "a.txt " will be renamed to five spaces. Patterns are not applied, renaming is always done only once.
5) The file is reopened with the "File.truncate" flag. That is, when opening a file, the OS will trim this file to zero size (it will be harder to understand how much the file weighed)
6) The file is deleted by the usual means of the OS


flag 'v' switches to verbosive mode

flag 'vv' switches to twice verbosive mode

flag 'pr' do show progress

flag 'sl' get slow down progress (pauses when using the disk)

flag 'cr' set to creation mode. A not existence file must be created as big as possible

flag 'crd' set to creation mode with create a many count of directories.
To clean up the inode, it is better to use other solutions, for example, "sfill -fllvzi /" from the "secure-delete" package

Directories are created without patterns, patterns are not applied.

flag 'ndd' - do not delete directories

"crs" or "crds" set to creation mode without additional file overwriting.

"cr" creates a file with the pattern 0x00 (zero). Then the program directs the file to wipe as if the program would have been called to wipe file.
This means that the empty space on the disk is first filled with zeros and then re-filled again with the usual pattern.

For "crs" the file is created with the template 0x00. And then it just gets deleted. Thus, wiping is carried out only once.

"crf" starts the program so that it creates only directories (a large number) without creating a large file to wipe empty space.


use ':' to use with conveyor.
Example:
ls -1 | sdel 'v:-'


## Usage examples

Overwriting the swap file if it is located at /swapfile

sdel prv /swapfile


If you want to overwrite the file with zeros, use the "z" flag

sdel z_prv /swapfile


Overwriting on a hard disk (magnetic disk, no for hybrid and no for SSD) can be done once.
Overwriting a file on hybrids and SSD (and flash memory) is useless, since the writing goes to other memory cells.
Such a one-time overwrite will prevent the file from being restored only programmatically,
but if there is equipment for physical connection to the microcontroller, then the file will be restored.


Overwriting an empty space on a hard (magnetic) disk.

sdel crds_prv_sl ~/_toErase


After such a rewrite, it is recommended to clear the inode. This can be done by the sfill program mentioned above.
"sl" will slow down the overwriting, since it will insert pauses. This will avoid significantly slowing down for other programs.
"~/_toErase" is a non-existent directory that will be created by the program. It should be located on the disk that we want to overwrite.


Overwriting an empty space on an SSD or flash drive (the essence is the same only in different cases).

sudo sdel crd_prv_sl ~/_toErase

The explanations are similar to those given above. Inode cleanup using sfill is also required.

Pressing ctrl+c will cause a stop the program. You can to delete the created files manually.

If the cursor disappears after removing the program, run the program without parameters:

sdel


# Русский
На Linux требует установленной [.NET 7.0](https://dotnet.microsoft.com/download)

Построение для вашей системы

    cd you_directory_for_sdel
    
    git clone https://github.com/VinnySmallUtilities/sdel
    
    cd sdel
    
    dotnet publish --output ./build -c Release --self-contained false /p:PublishSingleFile=true


Программа удаляет файл (папку) с однократным перезатированием данных в нём.
Используется простая перезапись файла через функции ОС, не ждите ничего особенного.

Флаги вводятся нестандартным образом. Например:

sdel vvz2pr /home/user/.wine

Это флаги vv, z2, pr


Эти же флаги можно ввести как

sdel vv_z2_pr /home/user/.wine

sdel "vv z2 pr" /home/user/.wine


Запуск без флагов

sdel - /home/user/.wine


Удаление файла происходит следующим образом:
1) Перезатирается каждый байт файла, но не далее его конца (например, файл, длиной 24 байта, перезатрётся 24-мя байтами)

    Шаблон перезатирания: 0x55AA (0101010110101010b)
    
    С флагом z шаблон перезатирания 0x00 (ноль)
    
    Шаблоны перезатирания с флагом z2: 0x55AA и 0x0000

    Шаблоны перезатирания с флагом z3: (0xCCCC 0x6666 0x00)
    
    1100110011001100
    
    0110011001100110
    
    0000000000000000

2) Размер блока файловой системы не известен программе. Она работает на слишком высоком уровне.
3) Размер файла дополняется до кратного 65536 байтам (0x10000) тем же шаблоном
    Если перезатирание идёт несколько раз, то перезатирание дополнительного пространства идёт уже после того, как выполнены все перезатирания всего файла.
4) Файл переименовывается. Символы имени файла однократно перезатираются пробелами с той же длиной. Например, файл "a.txt" будет переименован в имя, состоящее из пяти пробелов. Шаблоны не применяются, переименование всегда осуществляется только один раз.
5) Файл переоткрывается с флагом File.truncate. То есть при открытии файла, ОС обрежет этот файл до нулевого размера (тяжелее будет понять, сколько весил файл)
6) Файл удаляется обычыми средствами ОС

Флаг "v" включает разговорчивый режим. "vv" удваивает разговорчивость.

Флаг "pr" показывает прогресс перезаписи

Флаг "sl" вставляет паузы в работу с диском. Иногда это позволяет избежать фатального замедления остальных программ

Флаг "cr" указывает программе создать большой файл. Программа создаст директорию с именем, указанным как параметр.

"crd" дополнительно создаст в указанной директории множество поддиректорий, чтобы лучше перезаписать пустое пространство на диске.
Для очистки inode лучше использовать другие решения, например "sfill -fllvzi /" из пакета "secure-delete"
Директории создаются с именеми "как получится", шаблоны не применяются.

"crs" или "crds" создаст режим без дополнительной перезаписи файла.

"cr" создаёт файл с шаблоном 0x00 (ноль). Потом направляет его на перезатирание так, как если бы программа была бы вызвана для его перезатирания.
Это означает, что пустое пространство на диске сначала перезатирается нулями и потом перезатирается ещё раз обычным шаблоном.

Для "crs" файл создаётся с шаблоном 0x00. И потом просто удаляется. Таким образом перезатирание осуществляется только однократно.

"crf" запускает программу для того, чтобы она создала только директории (большое количество) без создания большого файла для перезатирания пустгого места.

Флаг 'ndd' - программа не будет удалять директории

Используйте двоеточие ':' вместе с конвейером команд.
Пример:
ls -1 | sdel 'v:-'


## Примеры использования

Перезапись файла подкачки, если он расположен по адресу /swapfile

sdel prv /swapfile


Если вы хотите перезаписать файл нулями, используйте флаг "z"

sdel z_prv /swapfile


Перезапись на жёстком диске (магнитном диске, не гибриде и не SSD) может производиться только один раз.
Перезапись файла на гибридах и SSD (и флеш-памяти) бесполезна, так как запись идёт в другие ячейки памяти.
Такая однократная перезапись предотвратит восстановление файла только программным способом,
но если имеется оборудование для физического подключения к микроконтроллеру, то файл удастся восстановить.


Перезапись пустого места на жёстком (магнитном) диске.

sdel crds_prv_sl ~/_toErase

После такой перезаписи рекомендуется очистить inode. Это можно сделать упомянутой выше программой sfill.
"sl" замедлит перезапись, так как будет вставлять паузы. Это позволит избежать существенного замедления других программ.
"~/_toErase" - это несуществующая директория, которая будет создана программой. Она должна быть расположена на том диске, который мы хотим перезаписать.


Перезапись пустого места на SSD или флеш-накопителе (суть одно и то же только в разных корпусах).

sudo sdel crd_prv_sl ~/_toErase

Разъяснения аналогичны приведённым выше. Также требуется очистка inode с помощью sfill.


Нажатие ctrl+c прекращает работу программы. Удалите созданные файлы вручную.

Если после снятия программы пропал курсор, запустите программу без параметров:

sdel
