cmake_minimum_required (VERSION 3.15)
project (env)

add_executable(get_host_info get_host_info.cpp)
add_executable(is_in_job is_in_job.c)
add_executable(make_memory_load make_memory_load.c)
add_executable(run_in_job run_in_job.c)

if(MSVC)
  add_compile_options(/W4 /WX)
else()
  add_compile_options(-Wall -Wextra -pedantic -Werror)
endif()
