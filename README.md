# Astra Browser

Лёгкий настольный браузер для Windows на современном движке **WebView2 (Chromium)**. Версия 7.7. Собирается из исходного кода в один `.exe` штатным компилятором C# — без Visual Studio и без установки NuGet-пакетов.

## Возможности

- **Вкладки** — кнопка закрытия на вкладке, закрытие средним/правым кликом, сохранение сессии между запусками.
- **Закладки** — добавление, просмотр списком, переход двойным кликом.
- **История** посещений с очисткой.
- **Поисковая строка** (Яндекс) и **настраиваемая главная страница** — отдельный HTML-файл `home.html`, который можно править без пересборки.
- **Живые часы, дата, быстрые ссылки** и **логотип** на главной странице.
- **Страница приветствия** после установки (`welcome.html`) — показывается один раз.
- **Установщик** — копирует файлы в `AppData\Local\Astra Browser` и создаёт ярлыки на рабочем столе и в меню «Пуск».
- **Иконка приложения** и собственная иконка сайта (генерируются скриптом).

## Скриншот

![Логотип](logo.png)
![Гифка](Запись 2026-11-01 113851.mp4)

## Технологии

- **C# / .NET Framework 4.x**, Windows Forms
- **Microsoft.Web.WebView2** (Chromium)
- Без внешних зависимостей при сборке — компиляция через `csc.exe`

## Сборка

Компиляция браузера из исходника:

```
csc.exe /target:winexe /win32icon:astra_icon.ico /out:"Astra Browser.exe" ^
  /reference:Microsoft.Web.WebView2.Core.dll ^
  /reference:Microsoft.Web.WebView2.WinForms.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll mybrowser.cs
```

Сборка установщика:

```
csc.exe /target:winexe /out:"Установка Astra Browser.exe" ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.dll installer.cs
```

## Требования

- **Windows 10/11**
- **WebView2 Runtime** (Evergreen) — на современных Windows обычно уже установлен. Скачать при необходимости: [developer.microsoft.com/microsoft-edge/webview2](https://developer.microsoft.com/microsoft-edge/webview2)

## Структура проекта

| Файл | Назначение |
|------|-----------|
| `mybrowser.cs` | Исходный код браузера |
| `installer.cs` | Исходный код установщика |
| `makeicon.cs` | Генератор иконки приложения и логотипа |
| `home.html` | Главная страница (редактируемая) |
| `home_77.html` | Прототип главной (архив) |
| `theme_classic.html` | Тема «Классическая» |
| `theme_blue.html` | Тема «Синяя» |
| `theme_light.html` | Тема «Светлая» |
| `settings.html` | Экран настроек |
| `files.html` | Файловый менеджер (вкладка) |
| `welcome.html` | Страница приветствия после установки |
| `license.html` | Лицензионное соглашение (Ctrl+Shift+L) |
| `start.html` | Справочная страница-руководство |
| `astra_logo.png` | Логотип Astra |
| `astra_icon.ico` | Иконка приложения |
| `Microsoft.Web.WebView2.*.dll` | Библиотеки WebView2 для запуска |
| `WebView2Loader.dll` | Загрузчик WebView2 (обязателен рядом с exe) |

> Примечание: `mybrowser_data.txt` и папка `webview2_data` создаются автоматически при первом запуске и содержат вкладки/закладки/историю — их можно удалять, они пересоздадутся.

## Запуск

Проще всего использовать установщик (`Установка Astra Browser.exe`) — он скопирует файлы и создаст ярлыки. Либо запустить `Astra Browser.exe` прямо из папки.

## Лицензия

Проект предоставляется «как есть», без каких-либо гарантий. Разрешено свободное использование и модификация.
