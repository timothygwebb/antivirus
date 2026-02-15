#include <stdio.h>
#include "scanner.h"
#include "logger.h"

int main(void)
{
    log_info("Program started");

    const char* input = "example input";
    scan(input);

    log_info("Program finished");
    return 0;
}