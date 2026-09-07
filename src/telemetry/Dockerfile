FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /workspace
COPY src/telemetry/pom.xml ./pom.xml
RUN mvn -q -DskipTests dependency:go-offline
COPY src/telemetry/src ./src
RUN mvn -q -DskipTests package

FROM eclipse-temurin:21-jre
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /workspace/target/agrocontrol-telemetry-*.jar app.jar
EXPOSE 8100
HEALTHCHECK --interval=10s --timeout=3s --retries=5 CMD curl --fail --silent http://localhost:8100/health || exit 1
ENTRYPOINT ["java", "-jar", "/app/app.jar"]
