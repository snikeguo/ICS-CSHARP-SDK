if(NOT DEFINED OBJDUMP OR NOT DEFINED NM OR NOT DEFINED INPUT_ELF OR NOT DEFINED OUTPUT_DISASM OR NOT DEFINED OUTPUT_SYMBOLS)
    message(FATAL_ERROR "Missing required variables: OBJDUMP, NM, INPUT_ELF, OUTPUT_DISASM, OUTPUT_SYMBOLS")
endif()

execute_process(
    COMMAND "${OBJDUMP}" -d -C -S "${INPUT_ELF}"
    OUTPUT_FILE "${OUTPUT_DISASM}"
    RESULT_VARIABLE rv_objdump
)
if(NOT rv_objdump EQUAL 0)
    message(FATAL_ERROR "objdump failed with code ${rv_objdump}")
endif()

execute_process(
    COMMAND "${NM}" -n "${INPUT_ELF}"
    OUTPUT_FILE "${OUTPUT_SYMBOLS}"
    RESULT_VARIABLE rv_nm
)
if(NOT rv_nm EQUAL 0)
    message(FATAL_ERROR "nm failed with code ${rv_nm}")
endif()
