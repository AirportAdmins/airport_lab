# airport_lab

В корне проекта лежат start.bat (Windows) и start.sh (OS X) - скрипты для последовательного запуска нескольких компонент с небольшой задержкой


Использование в cmd:
`location-of-repository> start.bat 2.2`

Использование в shell:
`$ chmod 700 start.sh  
$ ./start.sh 2.2`


Где 2.2 - пример версии .NetCoreApp, можно опустить, тогда будет использоваться 3.0

Компоненты прописываются в components.txt, который также находится в корне

Несколько проектов можно запускать одновременно и в самой Visual Studio с помощью Multiple Startup Projects

Каждая ПК разрабатывается в отдельной ветке:

|Ветка|Компонента|
|-----|----------|
|comp/passenger      | Пассажир|  
|comp/cashbox        | Касса|  
|comp/registration   | Служба регистрации|  
|comp/schedule       | Расписание|  
|comp/timetable      | Табло|  
|comp/airplane       | Самолет|  
|comp/groundservice  | Служба наземного обслуживания (СНО)|  
|comp/groundmotion   | Управление наземным движением (УНД)|  
|comp/bus            | Пассажирский автобус|  
|comp/baggage        | Багажная машина|  
|comp/followme       | Follow me|  
|comp/catering       | Catering|  
|comp/deicing        | Deicing|  
|comp/fueltruck      | Топливозаправщик|  
|comp/storage        | Накопитель (пассажирский и багажный)|  
|comp/timeservice    | Служба времени|  
|comp/visualizer     | Визуализатор|  
|comp/logs           | Логи|  
