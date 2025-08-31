# LunaLyrics

[![License: GPL](https://img.shields.io/badge/License-GPL-yellow.svg)](LICENSE)

LunaLyrics는 현재 재생 중인 음악을 감지하여 **Project Moon 스타일 연출**로 가사를 화면에 표시해주는 실시간 가사 오버레이 프로그램입니다.


<br>

![실행 화면](https://github.com/user-attachments/assets/c1eea38d-ae65-4394-b47a-9a1f11038504)

<br>

## Features
* **실시간 미디어 감지**
    * 현재 PC에서 재생 중인 미디어(음악)의 정보와 재생 시간을 실시간으로 감지합니다.

* **가사 출력**
    * 아래 조건을 모두 만족하는 음악에 한해 가사를 자동으로 화면에 표시합니다.
        * LRCLIB에 정확히 같은 아티스트와 제목을 보유한 데이터가 있는 경우
        * LRCLIB의 데이터에 타임스탬프가 포함된 가사 데이터가 있는 경우
        * LRCLIB의 재생 시간 데이터와 미디어의 재생 시간이 오차범위 내에 있는 경우

* **API 호출 최적화**
    * 한 번 조회한 가사 데이터는 로컬에 캐싱하여 불필요한 API 호출을 최소화하고 응답 속도를 향상시킵니다. (개발 중, 현재 프로그램 종료 시 캐싱된 데이터 저장 안됨)

* **사용자 커스텀 기능** (개발 예정)
    * 가사 텍스트의 색상을 변경할 수 있습니다. (개발 중, 현재 랜덤 색상만 지원)
    * 가사 텍스트의 크기를 변경할 수 있습니다. (현재 고정 크기만 지원)
    * 가사 텍스트의 애니메이션 강도를 변경할 수 있습니다. (현재 고정 강도만 지원)
    * 가사 텍스트의 최대 길이를 변경할 수 있습니다. (현재 공백 포함 50자 고정, 초과 시 자동 분할)

## Download
완성된 프로그램은 아래 **GitHub Releases** 페이지에서 다운로드할 수 있습니다.
**[최신 버전 다운로드 (GitHub Releases)](https://github.com/your-username/LunaLyrics/releases)**

다운로드 후 압축을 풀고 `LunaLyrics.exe` 파일을 실행하세요.

## How to Use
1.  `LunaLyrics.exe`를 실행합니다.
2.  Spotify, YouTube Music 등 평소 사용하시는 음악 플레이어로 음악을 재생합니다.
3.  재생 중인 음악이 **[가사 출력]** 조건을 만족하면, 화면에 자동으로 가사가 나타납니다.

## Tech Stack
* **Main Engine**: `Unity`
* **Lyrics Data**: `LRCLIB`

## Special Thanks
**Project Moon**\
**[LRCLIB](https://lrclib.net/)**
