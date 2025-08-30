# LunaLyrics

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**LunaLyrics는 현재 재생 중인 음악을 감지하여 Project Moon 스타일의 연출로 가사를 화면에 표시해주는 실시간 가사 오버레이 프로그램입니다.**
사용자가 즐겨 사용하는 플레이어(Spotify, YouTube Music 등)와 함께 동작하며 독창적인 타이포그래피 아트 경험을 제공합니다.

<br>

[실행 화면 GIF]

<br>

## Features
* **실시간 미디어 감지**
    * 현재 PC에서 재생 중인 미디어(음악)의 정보와 재생 시간을 실시간으로 감지합니다.

* **가사 출력**
    * 아래 조건을 모두 만족하는 음악에 한해 가사를 자동으로 화면에 표시합니다.
        * Musixmatch DB에 등록된 공식 음악 정보와 일치하는 경우
        * 음악의 총 길이가 DB 정보와 오차 범위 내에 있는 경우
        * Musixmatch에서 실시간 가사를 지원하는 경우

* **API 호출 최적화**
    * 한 번 조회한 가사 데이터는 로컬에 캐싱하여 불필요한 API 호출을 최소화하고 응답 속도를 향상시킵니다.

* **사용자 커스텀 기능**
    * 가사 텍스트의 색상을 원하는 대로 변경할 수 있습니다.

## Download
완성된 프로그램은 아래 **GitHub Releases** 페이지에서 다운로드할 수 있습니다.
**[최신 버전 다운로드 (GitHub Releases)](https://github.com/your-username/LunaLyrics/releases)**

다운로드 후 압축을 풀고 `LunaLyrics.exe` 파일을 실행하세요.

## How to Use
1.  `LunaLyrics.exe`를 실행합니다.
2.  설정창에 개인 Musixmatch API 키를 입력합니다. (최초 1회)
3.  Spotify, YouTube Music 등 평소 사용하시는 음악 플레이어로 음악을 재생합니다.
4.  재생 중인 음악이 **[가사 출력]** 조건을 만족하면, 화면에 자동으로 가사가 나타납니다.

## Tech Stack
* **Main Engine**: `Unity`
* **Lyrics Data**: `Musixmatch API`
