@echo off
rem Builds lpsolve55.dll, x64, with the C runtime linked in, from the lp_solve 5.5 sources the QuadWild fork carries.
rem Usage: build_lpsolve55.bat <lp_solve_5.5 source folder>
rem The flags are those of lp_solve's own lpsolve55\cvc8msvcrt.bat, with /MT in place of /MD.
if "%~1"=="" (
    echo Usage: build_lpsolve55.bat ^<lp_solve_5.5 source folder^>
    exit /b 1
)
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
set L=%~1
set src=%L%\lp_MDO.c %L%\shared\commonlib.c %L%\shared\mmio.c %L%\shared\myblas.c %L%\ini.c %L%\colamd\colamd.c %L%\lp_rlp.c %L%\lp_crash.c %L%\bfp\bfp_LUSOL\lp_LUSOL.c %L%\bfp\bfp_LUSOL\LUSOL\lusol.c %L%\lp_Hash.c %L%\lp_lib.c %L%\lp_wlp.c %L%\lp_matrix.c %L%\lp_mipbb.c %L%\lp_MPS.c %L%\lp_params.c %L%\lp_presolve.c %L%\lp_price.c %L%\lp_pricePSE.c %L%\lp_report.c %L%\lp_scale.c %L%\lp_simplex.c %L%\lp_SOS.c %L%\lp_utils.c %L%\yacc_read.c
cl /nologo -W1 -I%L% -I%L%\shared -I%L%\bfp -I%L%\bfp\bfp_LUSOL -I%L%\bfp\bfp_LUSOL\LUSOL -I%L%\colamd /LD /MT /O2 /Zp8 /Gz -D_WINDLL -DWIN32 -D_CRT_SECURE_NO_DEPRECATE -D_CRT_NONSTDC_NO_DEPRECATE -DYY_NEVER_INTERACTIVE -DPARSER_LP -DINVERSE_ACTIVE=INVERSE_LUSOL -DRoleIsExternalInvEngine %src% %L%\lp_solve.def /Felpsolve55.dll
if exist *.obj del *.obj
