# VRChatScraper
VRChatScraper is a **deprecated** proof-of-concept asset scraper for downloading and archiving VRC assets, such as worlds and avatars.
This repo is published purely to provide a basic usage of the VRChat API.

## Background
This project began development in February 2021, being created to archive interesting and fun VRChat worlds, alongside recovering uploaded avatars in the case that their Unity projects were corrupted.
The capabilities of the application were expanded to meet the API's basic functions, and from there development stopped.
Since then, the VRChat API has expanded and changed, alongside an increase in security of assets.
Thus, I now have the confidence to publish this project without the fear of it being used maliciously.
All code has been freshly commented.

## Prerequisites
This project makes use of <a href="https://github.com/JamesNK/Newtonsoft.Json">Newtonsoft.Json</a>, <a href="https://github.com/silkfire/Pastel">Pastel</a>, and squid-box's <a href="https://github.com/squid-box/SevenZipSharp">SevenZipSharp</a> fork.
This project also makes use of a deprecated & modified (asynchronous) version of <a href="https://github.com/vrchatapi/vrchatapi-csharp">VRChatApiSharp</a>, which is included as a binary within the Dependencies folder.

## Disclaimer
As is stands, this application exceeds the recommended external VRChat API access limit of 60 seconds between calls.
Therefore, the account that you utilize this application with is at risk of being banned for API misuse.
**I am not responsible for any resultant bans or other consequences that come along with the usage of this project. It is purely meant as a proof-of-concept, not an optimized, complete, or compliant project for everyday users.**

Additionally, this is **not** a utility for avatar ripping.
**This application contains no method for circumventing VRChat's asset protection, nor will it ever.**
As of January 2022, in regards to avatars, you can only access the assets for your own avatars; all other avatar JSON responses include blank UnityPackage sections.
