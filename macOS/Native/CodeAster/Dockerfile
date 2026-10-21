# Code_Aster for PrePoMax on macOS: the conda-forge build for x86-64, which runs under Rosetta on Apple Silicon (the
# images of simvia are built for AMD Zen processors and do not). Built by build.sh as the image prepomax/code_aster.
FROM --platform=linux/amd64 condaforge/miniforge3:24.11.3-2
RUN mamba install -y -c conda-forge code-aster=18.1 && mamba clean -afy
WORKDIR /work
