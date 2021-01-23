# LunchBot

A Microsoft Teams bot that send you what's for lunch.

### Prerequisites

LunchBot was designed for employees of Hexagon at the Huntsville, AL location; however, feel free to extend the project using your own serialization.

The PDF Parser library was built in parallel to easily serialize the distributed menu.

### Ability

LunchBot is able to serve the weekly menu in two ways: as in body text or as a hyperlink to where the bot saved the PDF locally. The PDF Parser timestamps the serialized data and only fetches the file locally or the server when absolutely needed. This method avoids any unnecessary waiting...*We're hungry people!*

### Extensibility

In the future/proposed work for LunchBot:
- Sharing the menu with friends
- Emailing the cafe manager
- Filtering menu data