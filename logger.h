#ifndef LOGGER_H
#define LOGGER_H

typedef enum {
    LOG_INFO,
    LOG_WARNING,
    LOG_ERROR,
    LOG_RESULT
} LogLevel;

void log_message(LogLevel level, const char* format, ...);
void log_info(const char* format, ...);
void log_warning(const char* format, ...);
void log_error(const char* format, ...);
void log_result(const char* format, ...);

#endif // LOGGER_H