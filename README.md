# Elsa Workflows con Autenticación por OAuth 2.0 con Microsoft Entra ID
Implementación de una solución .Net 8.0 con Elsa Workflows Server v3 (elsa-core) y Elsa Workflows Studio (elsa-studio) v3 sobre Blazor Server (no funciona con Blazor WebAssembly).

Referencia: https://github.com/elsa-workflows/elsa-core/discussions/4814#discussioncomment-9398844

# Configuración en Microsoft Entra ID

1. Se deben crear Registros de Aplicaciones con configuración básica separados, uno para Elsa Workflows Server y otro para Elsa Workflows Studio.
2. Crear grupo para usuarios autenticados, por ejemplo WorkflowsAdmins y agregar los usuarios de Microsoft Entra Id que deben tener acceso de administración (único rol).

## Configuración de Registro de Aplicación para Backend (Server)

### Registro de Aplicaciones
1. Registro de aplicación para Backend
2. Registro de aplicación para Frontend

Conservar los siguientes datos:
-	Id de Aplicación (ClientId) para la aplicación de Backend y de Frontend
-	Id de directorio (TenantId)
 
Creadas las aplicaciones, se procede a la creación del Grupo de Seguridad que deberá asignarse para otorgar acceso a la aplicación.
 
#### Configuración de aplicación registrada para Backend

1.	Desde Registros de Aplicaciones, ir a la aplicación para Backend y luego a la sección Administrar > Exponer una API. 
2.	Agregar un ámbito (Scope).
3.	Seleccionar Administradores y usuarios para que ambos puedan dar el consentimiento.
4.	Llenar los campos de nombre y descripción.
5.	Tildar el estado como Habilitado.
6.	Al finalizar la creación del ámbito, se debe copiar la URI de la aplicación y del ámbito y conservarlas para utilizarla luego.
7.	Definir un rol para el Backend desde el Registro de la aplicación, sección Administrar > Roles de aplicación con permisos para Usuarios o grupos.
 
#### Configuración de aplicación registrada para Frontend

1.	Desde Registros de Aplicaciones, ir a la aplicación para Frontend y luego a la sección Administrar > Permisos de API.
2.	Hacer clic en Agregar un permiso.
3.	Seleccionar la pestaña API usadas en mi organización y seleccionar la API de la aplicación de Backend. 
4.	Luego de seleccionar la API correspondiente, tildar Permisos delegados y el checkbox del permiso.
5.	Definir un rol para el Frontend desde el Registro de la aplicación, sección Administrar > Roles de aplicación con permisos para Usuarios o grupos.
6.	Desde el Registro de la aplicación de Frontend, sección Administrar > Certificados y secretos, crear un nuevo secreto de cliente y conservar el valor, y dependiendo del tiempo de expiración establecido, generar el recordatorio necesario para su renovación.
7.	Desde el registro de la aplicación de Frontend, sección Administrar > Autenticación, agregar una nueva configuración de plataforma, seleccionando Web.
8.	Luego se debe ingresar la URI de redirección como https://<host>/signin-oidc y se deben seleccionar los tokens de acceso y de Id para que sean emitidos por el punto de conexión de autorización.
 
#### Mapeo de Roles para las Aplicaciones Registradas

1.	Desde la página principal de Microsoft Entra, abrir Aplicaciones empresariales.
2.	Abrir cada aplicación, Backend y Frontend, y luego desde la sección Administrar, ingresar en Usuarios y grupos.
3.	Agregar los grupos creados previamente y asignarles el rol correspondiente para cada aplicación.

## NOTA
En la raíz del proyecto se deben clonar los proyectos workflows-core (Rama EntityFrameworkWithNVARCHAR2) y workflows-studio (Rama SecretModels) dado que se usan las librerías de estos proyectos actualizados.
