// From https://github.com/FlynnOwen/GOL-C/blob/master/GOL.c
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

char boardInitState = 'r';
int i, j;
int counter;
int nRows = 40;
int nCols = 40;
char board[40][40];
char blankBoard[40][40];
int evolutions = 100;
int timeBetweenEvolutions = 20000;
int randomSeed = 50;
char cellAscii = 'O';

void initBoard(int boardInitState){
    // blank board
    if (boardInitState == 'b') {
        for (i = 0; i < nRows; i++){
            for (j = 0; j < nCols; j++){
            blankBoard[i][j] = ' ';
            }
        }
    }
    // board with random cells initiated
    else if (boardInitState == 'x') {
        for (i = 0; i < nRows; i++) {
            for (j = 0; j < nCols; j++) {
                board[i][j] = ' ';
            }
        }
    }
    // board with random cells initiated
    else if (boardInitState == 'r') {
        srand(randomSeed);
        int x;
        for (i = 0; i < nRows; i++){
            for (j = 0; j < nCols; j++){
                x = rand() % 2;
                if (x == 1) {
                    board[i][j] = cellAscii;
                }
                else {
                    board[i][j] = ' ';
                }
            }
        }
    }
    // board with just glider gun initiated
    else if (boardInitState == 'g') {
        for (i = 0; i < nRows; i++){
            for (j = 0; j < nCols; j++){
            board[i][j] = ' ';
            }
        }
        
        board[1][25] = cellAscii;
        board[2][23] = cellAscii;
        board[2][25] = cellAscii;
        board[3][13] = cellAscii;
        board[3][14] = cellAscii;
        board[3][21] = cellAscii;
        board[3][22] = cellAscii;
        board[3][35] = cellAscii;
        board[3][36] = cellAscii;
        board[4][12] = cellAscii;
        board[4][16] = cellAscii;
        board[4][21] = cellAscii;
        board[4][22] = cellAscii;
        board[4][35] = cellAscii;
        board[4][36] = cellAscii;
        board[5][1] = cellAscii;
        board[5][2] = cellAscii;
        board[5][11] = cellAscii;
        board[5][17] = cellAscii;
        board[5][21] = cellAscii;
        board[5][22] = cellAscii;
        board[6][1] = cellAscii;
        board[6][2] = cellAscii;
        board[6][11] = cellAscii;
        board[6][15] = cellAscii;
        board[6][17] = cellAscii;
        board[6][18] = cellAscii;
        board[6][23] = cellAscii;
        board[6][25] = cellAscii;
        board[7][11] = cellAscii;
        board[7][17] = cellAscii;
        board[7][25] = cellAscii;
        board[8][12] = cellAscii;
        board[8][16] = cellAscii;
        board[9][13] = cellAscii;
        board[9][14] = cellAscii;
    }

}

int _countCellNeighbours(int i, int j){
    int counter = 0;

    if (i - 1 > 0) {
        if (board[i-1][j] == cellAscii) {
            counter += 1;
        }
        if (j - 1 > 0) {
            if (board[i-1][j-1] == cellAscii) {
                counter += 1;
            }
        }
        if (j + 1 < nCols) {
            if (board[i-1][j+1] == cellAscii) {
                counter += 1;
            }
        }
    }

    if (i + 1 < nRows) {
        if (board[i+1][j] == cellAscii) {
                counter += 1;
        }

        if (j - 1 > 0) {
            if (board[i+1][j-1] == cellAscii) {
                counter += 1;
            }
        }
        if (j + 1 < nCols) {
            if (board[i+1][j+1] == cellAscii) {
                counter += 1;
            }
        }
    }


    if (j + 1 < nCols) {
        if (board[i][j+1] == cellAscii) {
            counter += 1;
        }
    }

    if (j - 1  > 0) {
        if (board[i][j-1] == cellAscii) {
            counter += 1;
        }
    }

    return counter;

}

void _evolveCell(int i, int j, int counter) {
    
    if (board[i][j] == cellAscii) {
        if (counter < 2) {
            blankBoard[i][j] = ' ';
        }
        else if (counter == 2 || counter == 3) {
            blankBoard[i][j] = cellAscii;
        }
        else {
            blankBoard[i][j] = ' ';
        }
    }

    else {
        if (counter == 3) {
            blankBoard[i][j] = cellAscii;
        }
    }

}

void evolveBoard() {
    for (i = 0; i < nRows; i++){
        for (j = 0; j < nCols; j++){
            counter = _countCellNeighbours(i, j);
            _evolveCell(i, j, counter);
        }
    }
}

void updateOldBoard() {
    for (i = 0; i < nRows; i++){
        for (j = 0; j < nCols; j++){
            board[i][j] = blankBoard[i][j];
        }
    }
}

void printBoardState(int evolution) {
    printf("\033[H"); // Sets cursor to homestate
    char c;
    for (i = 0; i < nRows; i++){
        for (j = 0; j < nCols; j++){
            c = board[i][j];
            printf("%c ", c);
        }
        printf("\n");
    }

    printf("Generation = %i\n", evolution);
}

int main(int argc, char *argv[]) {
    int e;
    int opt; 
    printf("\033c"); // clears the terminal
    printf("\033[?25l"); // makes cursor invisible

    /*while ((opt = getopt(argc, argv, ":ge:c:t:s:x")) != -1)
    { 
        switch(opt) 
        {   case 'g': 
                boardInitState = 'g';
                break; 
            case 't': 
                timeBetweenEvolutions = atoi(optarg);
                break;
            case 'c': 
                cellAscii = optarg[0];
                break; 
            case 'e': 
                evolutions = atoi(optarg);
                break; 
            case 's': 
                randomSeed = atoi(optarg);
                break; 
            case ':': 
                printf("option needs a value\n"); 
                break; 
            case '?': 
                printf("unknown option: %c\n", optopt);
                break; 
        } 
    } */

    initBoard(boardInitState);

    for (e=0; e < evolutions; e++) {
        usleep(timeBetweenEvolutions);
        initBoard('b');
        evolveBoard();
        updateOldBoard();
        printBoardState(e);
    }

    printf("\033[?25h"); // makes cursor visible
    return 0;
}
