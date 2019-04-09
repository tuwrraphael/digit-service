# digit-service
[![Build Status](https://dev.azure.com/raphaelhauk/digit/_apis/build/status/tuwrraphael.digit-service?branchName=master)](https://dev.azure.com/raphaelhauk/digit/_build/latest?definitionId=1&branchName=master)
## Introduction
Digit is not a personal assistent. It creates and bundles timely and contextual relevant information for a user. It provides its data and services to the users personal devices or assistants.

### Digit can show for example
* when you need to leave home to get to an appointment in time
* weather the user arrvies early/late and how much
* real time public transport data for the users current journey to show on devices
* _when to get up in the morning (planned)_
* _which transport method to use (public transport, car, bicycle, motorcycle) depending on weather, appointment and travel preferences (planned)_
* _how the user spends time (work, chores, travelling, recreation activities, sleeping) (planned)_

### Digit data and services be consumed for example
* via the [digit webapp](https://github.com/tuwrraphael/digit-webapp), which is also the main interface to connect calendars or change preferences. It can even alert the user with web push notifications.
* via a device such as the [digit smartwatch](https://github.com/tuwrraphael/digit-watch), _an android smartwatch (wear interface planned)_, a smart wall clock or basically any other custom device you connect
* any other (web-)service can enrich its user experience by connecting to digit service

### Where does the data come from
* The user can connect Outlook and Google calendars as a data source for appointments ([CalendarService](https://github.com/tuwrraphael/CalendarService))
* [TravelService](https://github.com/tuwrraphael/TravelService) provides directions and routing apis by merging common providers like Google Maps together with users travel preferences and real time data
* The [digit android app](https://github.com/tuwrraphael/digit-app-android) provides the users location on demand using a low power battery effective method based on push messages and forecasting
