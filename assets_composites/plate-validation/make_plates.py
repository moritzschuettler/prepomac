#Writes the mid-surfaces of the validation models as BREP files:
#  square_plate.brep  the plate of 200 x 200 mm the Navier solution is known for
#  strip.brep         the strip of 200 x 20 mm, pulled and bent as a cantilever
#
#   PYTHONPATH=/opt/homebrew/lib python3 make_plates.py          (gmsh with its Python API, brew install gmsh)

import os

import gmsh

HERE = os.path.dirname(os.path.abspath(__file__))

for name, (length, width) in (("square_plate", (200.0, 200.0)), ("strip", (200.0, 20.0))):
    gmsh.initialize()
    gmsh.option.setNumber("General.Terminal", 0)
    gmsh.model.add(name)
    gmsh.model.occ.addRectangle(0, 0, 0, length, width)
    gmsh.model.occ.synchronize()
    gmsh.write(os.path.join(HERE, name + ".brep"))
    gmsh.finalize()
    print("written", name + ".brep")
