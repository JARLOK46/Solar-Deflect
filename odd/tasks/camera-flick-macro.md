# Camera flick macro
User-requested feature tracked under ODD.
Implement the new one-shot camera macro and configurable action buttons.

## Specs
1. **S1** — "Quiero que sea una nueva funcion"
2. **S2** — "Quiero que tambien se puedan configurar el boton de spam y el boton de color, y el boton."
3. **S3** — "debe ser como lo dijimos en el @Instructions.txt"
4. **S4** — "Una sola vez"

## Tasks
- T1 [S1-S4] Model persisted macro and hotkey settings — route: inline — commit: d0be02a
- T2 [S1-S4] Register configurable action hotkeys and execute one-shot camera flick — route: inline — commit: d0be02a
- T3 [S1-S4] Add settings UI, capture controls, and user-facing help — route: inline — commit: d0be02a
- T4 [S1-S4] Verify tests and build — route: inline — commit: d0be02a

## Log
- L1: "1- Quiero que sea una nueva funcion\n2- Quiero que tambien se puedan configurar el boton de spam y el boton de color, y el boton.\n3- debe ser como lo dijimos en el @Instructions.txt\n4- Una sola vez"
- L2: "El pogama tiene un desbodamiento de los elementos a la deecha, en ajustes y ayuda, tambien quieo que la Cuve macro este en otro apartado del dashboard en el sidebar y se configure desde ahi"
- L3: "Los botones deberian aparecer en ajustes, para configurarlos, en macro curva solo deberia quedar lo de esa macrco"
- L4: "Quiiero que subas los cambios al github, y generes el .exe, despues:\n\nQuiero que crees un AGENTS.md detallado de el programa y el flujo que se debe hacer, tambien quiero que detalles la parte de subir cambios a github, al finalizar, y crear el .exe en dist/TriggerBot-win-x64"
